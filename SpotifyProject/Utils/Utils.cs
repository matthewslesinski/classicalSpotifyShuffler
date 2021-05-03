using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace SpotifyProject.Utils
{
	/** Utility methods that are generic */
	public static class Utils
	{
		public static bool SequenceEquals<T>(this IEnumerable<T> sequence1, IEnumerable<T> sequence2) => SequenceEquals(sequence1, sequence2, (t1, t2)=> Equals(t1, t2));
		public static bool SequenceEquals<T>(this IEnumerable<T> sequence1, IEnumerable<T> sequence2, Func<T, T, bool> equalityMethod)
		{
			if (sequence1 == null || sequence2 == null)
				return sequence1 == sequence2;
			using (var enumerator1 = sequence1.GetEnumerator())
			using (var enumerator2 = sequence2.GetEnumerator())
			{
				while (enumerator1.MoveNext())
				{
					if (!enumerator2.MoveNext())
						return false;
					if (!equalityMethod(enumerator1.Current, enumerator2.Current))
						return false;
				}
				if (enumerator2.MoveNext())
					return false;
			}
			return true;
		}

		public static bool IsRomanNumeral(string possibleNumber, out RomanNumeral romanNumeral)
		{
			return RomanNumeral.TryParse(possibleNumber, out romanNumeral);
		}

		public static void Swap<T>(this T[] array, int index1, int index2)
		{
			var temp = array[index1];
			array[index1] = array[index2];
			array[index2] = temp;
		}

		public static IEnumerable<T> RandomShuffle<T>(this IEnumerable<T> sequence)
		{
			if (!sequence.Any())
				return Array.Empty<T>();
			var resultArray = sequence.ToArray();
			int[] randomNums;
			if (resultArray.Count() < byte.MaxValue)
			{
				var temp = new byte[resultArray.Count() - 1];
				ThreadSafeRandom.Generator.NextBytes(temp);
				randomNums = temp.Select(b => (int)b).ToArray();

			}
			else
				randomNums = Enumerable.Range(0, resultArray.Count() - 1).Select(i => ThreadSafeRandom.Generator.Next()).ToArray();
			
			for (var i = resultArray.Count() - 1; i > 0; i--)
				resultArray.Swap(i, randomNums[i - 1] % (i + 1));
			return resultArray;
		}

		public static IEnumerable<T> TraverseBreadthFirst<T>(this T seed, Func<T, IEnumerable<T>> branchingMechanism)
		{
			var q = new Queue<T>();
			q.Enqueue(seed);
			var seen = new HashSet<T>();
			while (q.TryDequeue(out var next))
			{
				if (seen.Contains(next))
					continue;
				yield return next;
				seen.Add(next);
				foreach (var child in branchingMechanism(next))
					q.Enqueue(child);
			}
		}

		public static IEnumerable<T> OrderBy<T>(this IEnumerable<T> sequence, IComparer<T> comparer) => sequence.OrderBy(x => comparer);

		public static async IAsyncEnumerable<R> RunInParallel<T, R>(this IEnumerable<T> sequence, Func<T, Task<R>> mapper, [EnumeratorCancellation] CancellationToken cancel = default)
		{
			var requests = sequence.Select(mapper).ToList();
			foreach (var request in requests)
			{
				cancel.ThrowIfCancellationRequested();
				yield return await request;
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
	}

	public static class ThreadSafeRandom
	{
		[ThreadStatic] private static Random Local;

		public static Random Generator
		{
			get { return Local ??= new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId)); }
		}
	}
}
