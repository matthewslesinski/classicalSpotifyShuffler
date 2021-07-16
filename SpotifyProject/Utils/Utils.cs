using System;
using System.Collections.Generic;
using System.Threading;
using SpotifyProject.Setup;
using System.Linq;
using System.Threading.Tasks;

namespace SpotifyProject.Utils
{
	/** Utility methods that are generic */
	public static class Utils
	{
		public static (A first, B second, C third) Append<A, B, C>(this (A first, B second) firstTwo, C third) => (firstTwo.first, firstTwo.second, third);
		public static (A first, B second, C third, D fourth) Append<A, B, C, D>(this (A first, B second, C third) firstThree, D fourth) => (firstThree.first, firstThree.second, firstThree.third, fourth);
		public static (A first, B second, C third, D fourth, E fifth) Append<A, B, C, D, E>(this (A first, B second, C third, D fourth) firstFour, E fifth) => (firstFour.first, firstFour.second, firstFour.third, firstFour.fourth, fifth);

		public static bool IsRomanNumeral(string possibleNumber, out RomanNumeral romanNumeral)
		{
			return RomanNumeral.TryParse(possibleNumber, out romanNumeral);
		}

		public static string Truncate(this string initialString, int? charLimit, string truncatedSuffix = "...") =>
			charLimit.HasValue && initialString != null && charLimit.Value >= 0 && initialString.Length > charLimit.Value
				? initialString.Substring(0, charLimit.Value) + truncatedSuffix
				: initialString;

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

		public static bool LoadOnce(ref bool isLoaded, object loadLock, Action loadAction)
		{
			if (!isLoaded)
			{
				lock(loadLock)
				{
					if (!isLoaded)
					{
						loadAction();
						isLoaded = true;
						return true;
					}
				}
			}
			return false;
		}

		public static async Task<bool> LoadOnceAsync(Func<bool> isLoadedGetter, Action<bool> isLoadedSetter, object loadLock, Func<Task> loadAction)
		{
			Task loadActionTask = null;
			if (!isLoadedGetter())
			{
				lock (loadLock)
				{
					if (!isLoadedGetter())
					{
						loadActionTask = loadAction();
					}
				}
			}
			if (loadActionTask != null)
			{
				await loadActionTask.WithoutContextCapture();
				isLoadedSetter(true);
				return true;
			}
			return false;
		}
	}

	public static class ThreadSafeRandom
	{
		[ThreadStatic] private static Random Local;
		private static readonly int? _hardSeed = Settings.Get<int?>(SettingsName.RandomSeed);

		public static Random Generator
		{
			get { return Local ??= new Random(_hardSeed ?? unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId)); }
		}
	}

	public static class Ensure
	{
		/// <summary>
		///   Checks an argument to ensure it isn't null.
		/// </summary>
		/// <param name = "value">The argument value to check</param>
		/// <param name = "name">The name of the argument</param>
		public static void ArgumentNotNull(object value, string name)
		{
			if (value != null)
			{
				return;
			}

			throw new ArgumentNullException(name);
		}
	}
}
