using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace SpotifyProject.Utils
{
	public static class LinqExtensions
	{
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

		public static IEnumerable<T> OrderBy<T>(this IEnumerable<T> sequence, IComparer<T> comparer) => sequence.OrderBy(x => x, comparer);

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

		public static IEnumerable<T> Maxima<T>(this IEnumerable<T> sequence) where T : IComparable => Maxima(sequence, Comparer<T>.Default);
		public static IEnumerable<T> Maxima<T>(this IEnumerable<T> sequence, IComparer<T> comparer) => Minima(sequence, comparer.Reverse());
		public static IEnumerable<T> Minima<T>(this IEnumerable<T> sequence) where T : IComparable => Minima(sequence, Comparer<T>.Default);
		public static IEnumerable<T> Minima<T>(this IEnumerable<T> sequence, IComparer<T> comparer)
		{
			if (sequence == null)
				return null;
			using (var enumerator = sequence.GetEnumerator())
			{
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
		}
	}
}
