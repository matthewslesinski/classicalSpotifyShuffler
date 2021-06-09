using System;
using System.Collections.Generic;
using System.Threading;
using SpotifyProject.Setup;

namespace SpotifyProject.Utils
{
	/** Utility methods that are generic */
	public static class Utils
	{
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


		public static IEnumerable<T> TraverseBreadthFirst<T>(T seed, Func<T, IEnumerable<T>> branchingMechanism)
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
	}

	public static class ThreadSafeRandom
	{
		[ThreadStatic] private static Random Local;
		private static readonly int? _hardSeed = GlobalCommandLine.Store.GetOptionValue<int?>(CommandLineOptions.Names.RandomSeed);

		public static Random Generator
		{
			get { return Local ??= new Random(_hardSeed ?? unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId)); }
		}
	}
}
