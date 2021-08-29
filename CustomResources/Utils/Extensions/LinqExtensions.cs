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

		public static void Each<T>(this IEnumerable<T> sequence, Action<T> action) { foreach (var item in sequence) action(item); }

		public static IEnumerable<(T item, int index)> Enumerate<T>(this IEnumerable<T> sequence) => sequence.Select((t, i) => (t, i));

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

		public static IReadOnlyDictionary<T, C> ToIndexMap<T, C>(this IEnumerable<T> sequence, IEqualityComparer<T> equalityComparer = null) where C : ICollection<int>, new() =>
			ToIndexMap(sequence, group => { var c = new C(); group.Each(c.Add); return c; }, equalityComparer);

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

		public static IEnumerable<(A first, B second, C third)> Zip<A, B, C>(this IEnumerable<A> sequence1, IEnumerable<B> sequence2, IEnumerable<C> sequence3) =>
			sequence1.Zip(sequence2).Zip(sequence3, (firstTwo, third) => firstTwo.Append(third));
		public static IEnumerable<(A first, B second, C third, D fourth)> Zip<A, B, C, D>(this IEnumerable<A> sequence1, IEnumerable<B> sequence2, IEnumerable<C> sequence3, IEnumerable<D> sequence4) =>
			sequence1.Zip(sequence2).Zip(sequence3, (firstTwo, third) => firstTwo.Append(third)).Zip(sequence4, (firstThree, fourth) => firstThree.Append(fourth));
		public static IEnumerable<(A first, B second, C third, D fourth, E fifth)> Zip<A, B, C, D, E>(this IEnumerable<A> sequence1, IEnumerable<B> sequence2, IEnumerable<C> sequence3, IEnumerable<D> sequence4, IEnumerable<E> sequence5) =>
			sequence1.Zip(sequence2).Zip(sequence3, (firstTwo, third) => firstTwo.Append(third)).Zip(sequence4, (firstThree, fourth) => firstThree.Append(fourth)).Zip(sequence5, (firstFour, fifth) => firstFour.Append(fifth));

	}
}
