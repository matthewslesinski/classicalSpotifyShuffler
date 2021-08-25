﻿using System;
using System.Collections.Generic;
using System.Threading;
using SpotifyProject.Setup;
using System.Linq;
using System.Threading.Tasks;
using SpotifyProject.Utils.Concepts;
using SpotifyProject.Utils.Extensions;
using System.Linq.Expressions;

namespace SpotifyProject.Utils.GeneralUtils
{
	/** Utility methods that are generic */
	public static class Utils
	{
		public static bool IsRomanNumeral(string possibleNumber, out RomanNumeral romanNumeral)
		{
			return RomanNumeral.TryParse(possibleNumber, out romanNumeral);
		}

		public static string Truncate(this string initialString, int? charLimit, string truncatedSuffix = "...") =>
			charLimit.HasValue && initialString != null && charLimit.Value >= 0 && initialString.Length > charLimit.Value
				? initialString.Substring(0, charLimit.Value) + truncatedSuffix
				: initialString;

		public static int BinarySearch<T>(Func<int, T> indexGetter, int startIndex, int endIndex, T queryItem, IComparer<T> comparer = null)
		{
			if (comparer == null)
				comparer = Comparer<T>.Default;

			int low = startIndex;
			int high = endIndex - 1;
			while (low <= high)
			{
				int mid = low + ((high - low) >> 1);
				int comparisonResult = comparer.Compare(indexGetter(mid), queryItem);
				if (comparisonResult == 0)
				{
					return mid;
				}
				if (comparisonResult < 0)
				{
					low = mid + 1;
				}
				else
				{
					high = mid - 1;
				}
			}
			return ~low;
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

		public static Random NewGenerator(out int seedUsed)
		{
			seedUsed = _msInternalSeedGenerator.Value();
			return new Random(seedUsed);
		}

		private static readonly Lazy<Func<int>> _msInternalSeedGenerator = new Lazy<Func<int>>(Expression.Lambda<Func<int>>(Expression.Call(null,
			typeof(Random).GetMethod("GenerateSeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)))
			.Compile());
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