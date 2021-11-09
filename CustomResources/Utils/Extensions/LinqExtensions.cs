using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Collections.Immutable;
using CustomResources.Utils.Concepts;

namespace CustomResources.Utils.Extensions
{
	public static class LinqExtensions
	{
		public delegate bool TryGetFunc<in K, V>(K key, out V value);

		public static IEnumerable<List<T>> Batch<T>(this IEnumerable<T> sequence, int batchSize)
		{
			using var enumerator = sequence.GetEnumerator();
			while (enumerator.MoveNext())
			{
				var firstElement = enumerator.Current;
				var list = new List<T>(batchSize) { firstElement };
				for (int i = 1; i < batchSize; i++)
				{
					if (enumerator.MoveNext())
						list.Add(enumerator.Current);
					else
						break;
				}
				yield return list;
			}
		}

		public static bool ContainsSameElements<T>(this IEnumerable<T> sequence1, IEnumerable<T> sequence2, IEqualityComparer<T> equalityComparer = null) =>
			ContainsSameElements(sequence1, sequence2, out _, equalityComparer);

		public static bool ContainsSameElements<T>(this IEnumerable<T> sequence1, IEnumerable<T> sequence2,
			out IEnumerable<(T element, int sequence1Count, int sequence2Count)> differences, IEqualityComparer<T> equalityComparer = null) =>
				!(differences = FrequencyDifferences(sequence1, sequence2, equalityComparer)).Any();

		public static IEnumerable<T> DistinctOrdered<T>(this IEnumerable<T> sequence, IEqualityComparer<T> equalityComparer = null)
		{
			var set = new HashSet<T>(equalityComparer ?? EqualityComparer<T>.Default);
			foreach (var element in sequence) if (set.Add(element)) yield return element;
		}

		public static IEnumerable<T> Each<T>(this IEnumerable<T> sequence, Action<T> action) { foreach (var item in sequence) action(item); return sequence; }
		public static IEnumerable<T> Each<T>(this IEnumerable<T> sequence, Action<T, int> action) { foreach (var (item, index) in sequence.Enumerate()) action(item, index); return sequence; }
		public static IEnumerable<T> EachIndependently<T>(this IEnumerable<T> sequence, Action<T> action)
		{
			List<Exception> exceptions = null;
			foreach (var item in sequence)
			{
				try
				{
					action(item);
				}
				catch (Exception e)
				{
					if (exceptions == null)
						exceptions = new List<Exception>();
					exceptions.Add(e);
				}
			}
			if (exceptions != null)
			{
				var exception = exceptions.Count > 1 ? new AggregateException(exceptions) : exceptions.Single();
				throw exception;
			}
			return sequence;
		}

		public static IEnumerable<(T item, int index)> Enumerate<T>(this IEnumerable<T> sequence) => sequence.Select((t, i) => (t, i));

		public static IEnumerable<T> FilterByOccurrenceNumber<T>(this IEnumerable<T> sequence, Func<T, int, bool> filter, IEqualityComparer<T> equalityComparer = null)
		{
			var countsSoFar = new Dictionary<T, int>(equalityComparer ?? EqualityComparer<T>.Default);
			foreach (var element in sequence)
			{
				var countSoFar = countsSoFar.Merge(element, 1, (i, j) => i + j);
				if (filter(element, countSoFar))
					yield return element;
			}
		}

		public static IEnumerable<(T element, int sequence1Count, int sequence2Count)> FrequencyDifferences<T>(this IEnumerable<T> sequence1,
			IEnumerable<T> sequence2, IEqualityComparer<T> equalityComparer = null)
		{
			equalityComparer ??= EqualityComparer<T>.Default;
			var sequence1Elements = sequence1.ToFrequencyMap(equalityComparer);
			var sequence2Elements = sequence2.ToFrequencyMap(equalityComparer);
			return sequence1.Union(sequence2, equalityComparer)
				.Select(element => (element, sequence1Elements.GetValueOrDefault(element, 0), sequence2Elements.GetValueOrDefault(element, 0)))
				.Where(triple => triple.Item2 != triple.Item3);
		}

		public static V Get<K, V>(this IReadOnlyDictionary<K, V> dictionary, K key) =>
			dictionary.TryGetValue(key, out var value) ? value : throw new KeyNotFoundException($"The given key was not found {key}");

		public static bool IsSubsequenceOf<T>(this IEnumerable<T> sequence, IEnumerable<T> possibleSuperSequence, IEqualityComparer<T> equalityComparer = null)
		{
			equalityComparer ??= EqualityComparer<T>.Default;
			using (var superSequenceEnumerator = possibleSuperSequence.GetEnumerator())
			{
				foreach (var element in sequence)
				{
					if (!superSequenceEnumerator.MoveNext())
						return false;
					while (!equalityComparer.Equals(superSequenceEnumerator.Current, element))
					{
						if (!superSequenceEnumerator.MoveNext())
							return false;
					}

				}
			}
			return true;
		}
		public static bool IsSuperSequenceOf<T>(this IEnumerable<T> sequence, IEnumerable<T> possibleSubSequence, IEqualityComparer<T> equalityComparer = null) =>
			possibleSubSequence.IsSubsequenceOf(sequence, equalityComparer);

		public static IEnumerable<T> KDistinct<T>(this IEnumerable<T> sequence, int k, IEqualityComparer<T> equalityComparer = null) =>
			sequence.KDistinct(_ => k, equalityComparer);
		public static IEnumerable<T> KDistinct<T>(this IEnumerable<T> sequence, Func<T, int> kDeterminer, IEqualityComparer<T> equalityComparer = null) =>
			sequence.FilterByOccurrenceNumber((element, occurrenceNumber) => occurrenceNumber <= kDeterminer(element), equalityComparer);

		public static IEnumerable<T> Maxima<T>(this IEnumerable<T> sequence) where T : IComparable => Maxima(sequence, Comparer<T>.Default);
		public static IEnumerable<T> Maxima<T>(this IEnumerable<T> sequence, IComparer<T> comparer) => Minima(sequence, comparer.Reversed());
		public static IEnumerable<T> Minima<T>(this IEnumerable<T> sequence) where T : IComparable => Minima(sequence, Comparer<T>.Default);
		public static IEnumerable<T> Minima<T>(this IEnumerable<T> sequence, IComparer<T> comparer)
		{
			if (sequence == null)
				return null;
			using var enumerator = sequence.GetEnumerator();
			if (!enumerator.MoveNext())
				return sequence;
			T min = enumerator.Current;
			List<T> minima = new List<T> { min };
			while (enumerator.MoveNext())
			{
				var candidate = enumerator.Current;
				var comparerResult = comparer.Compare(candidate, min);
				if (comparerResult < 0)
				{
					minima.Clear();
					min = candidate;
				}
				if (comparerResult <= 0)
					minima.Add(candidate);
			}
			return minima;
		}

		public static V Merge<K, V>(this IDictionary<K, V> dict, K key, V value, Func<V, V, V> remappingFunction) =>
			dict[key] = dict.TryGetValue(key, out var foundValue) ? remappingFunction(foundValue, value) : value;

		public static IEnumerable<T> OrderBy<T>(this IEnumerable<T> sequence, IComparer<T> comparer) => sequence.OrderBy(x => x, comparer);

		public static T[] RandomShuffle<T>(this IEnumerable<T> sequence, Random generator)
		{
			if (!sequence.Any())
				return Array.Empty<T>();
			var resultArray = sequence.ToArray();
			int[] randomNums;
			if (resultArray.Length < byte.MaxValue)
			{
				var temp = new byte[resultArray.Length - 1];
				generator.NextBytes(temp);
				randomNums = temp.Select(b => (int)b).ToArray();
			}
			else
				randomNums = Enumerable.Range(0, resultArray.Length - 1).Select(i => generator.Next()).ToArray();

			for (var i = resultArray.Length - 1; i > 0; i--)
				resultArray.Swap(i, randomNums[i - 1] % (i + 1));
			return resultArray;
		}

		public static List<T> Reversed<T>(this IEnumerable<T> sequence) { var list = sequence.ToList(); list.Reverse(); return list; }

		public static async IAsyncEnumerable<R> RunInParallel<T, R>(this IEnumerable<T> sequence, Func<T, Task<R>> mapper, [EnumeratorCancellation] CancellationToken cancel = default)
		{
			var requests = sequence.Select(mapper).ToList();
			foreach (var request in requests)
			{
				cancel.ThrowIfCancellationRequested();
				yield return await request.WithoutContextCapture();
			}
		}

		public static async IAsyncEnumerable<R> RunInParallel<T, R>(this IEnumerable<T> sequence, Func<T, ConfiguredTaskAwaitable<R>> mapper, [EnumeratorCancellation] CancellationToken cancel = default)
		{
			var requests = sequence.Select(mapper).ToList();
			foreach (var request in requests)
			{
				cancel.ThrowIfCancellationRequested();
				yield return await request;
			}
		}

		public static IReadOnlyDictionary<T, int> ToFrequencyMap<T>(this IEnumerable<T> sequence, IEqualityComparer<T> equalityComparer = null) =>
			sequence.ToIndexMap(group => group.Count(), equalityComparer);

		public static IReadOnlyDictionary<T, IEnumerable<int>> ToIndexMap<T>(this IEnumerable<T> sequence, IEqualityComparer<T> equalityComparer = null) =>
			ToIndexMap(sequence, indices => indices, equalityComparer);

		public static IReadOnlyDictionary<T, C> ToIndexMap<T, C>(this IEnumerable<T> sequence, Func<IEnumerable<int>, C> collectionCreator, IEqualityComparer<T> equalityComparer = null) =>
			sequence.Enumerate()
				.GroupBy(pair => pair.item, pair => pair.index, equalityComparer ?? EqualityComparer<T>.Default)
				.ToImmutableDictionary(group => group.Key, group => collectionCreator(group), equalityComparer ?? EqualityComparer<T>.Default);
		
		public static bool TryGetFirst<T>(this IEnumerable<T> sequence, out T result) => TryGetFirst(sequence, t => true, out result);
		public static bool TryGetFirst<T>(this IEnumerable<T> sequence, Func<T, bool> predicate, out T result) => TryGetFirst(sequence, (T item, out T returnedItem) => {
			if (predicate(item))
			{
				returnedItem = item;
				return true;
			}
			returnedItem = default;
			return false;
		}, out result);
		public static bool TryGetFirst<T, V>(this IEnumerable<T> sequence, TryGetFunc<T, V> predicate, out V result)
		{
			foreach (var element in sequence)
			{
				if (predicate(element, out var value))
				{
					result = value;
					return true;
				}
			}
			result = default;
			return false;
		}
		public static bool TryGetSingle<T>(this IEnumerable<T> sequence, out T result) => TryGetSingle(sequence, t => true, out result);
		public static bool TryGetSingle<T>(this IEnumerable<T> sequence, Func<T, bool> predicate, out T result)
		{
			bool foundElement = false;
			result = default;
			foreach (var item in sequence)
			{
				if (predicate(item))
				{
					if (foundElement)
						return false;
					foundElement = true;
					result = item;
				}
			}
			return foundElement;
		}

		public static IEnumerable<(A first, B second, C third)> Zip<A, B, C>(this IEnumerable<A> sequence1, IEnumerable<B> sequence2, IEnumerable<C> sequence3) =>
			sequence1.Zip(sequence2).Zip(sequence3, (firstTwo, third) => firstTwo.Append(third));
		public static IEnumerable<(A first, B second, C third, D fourth)> Zip<A, B, C, D>(this IEnumerable<A> sequence1, IEnumerable<B> sequence2, IEnumerable<C> sequence3, IEnumerable<D> sequence4) =>
			sequence1.Zip(sequence2).Zip(sequence3, (firstTwo, third) => firstTwo.Append(third)).Zip(sequence4, (firstThree, fourth) => firstThree.Append(fourth));
		public static IEnumerable<(A first, B second, C third, D fourth, E fifth)> Zip<A, B, C, D, E>(this IEnumerable<A> sequence1, IEnumerable<B> sequence2, IEnumerable<C> sequence3, IEnumerable<D> sequence4, IEnumerable<E> sequence5) =>
			sequence1.Zip(sequence2).Zip(sequence3, (firstTwo, third) => firstTwo.Append(third)).Zip(sequence4, (firstThree, fourth) => firstThree.Append(fourth)).Zip(sequence5, (firstFour, fifth) => firstFour.Append(fifth));

	}
}
