using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CustomResources.Utils.Concepts;
using CustomResources.Utils.Extensions;

namespace CustomResources.Utils.GeneralUtils
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
						isLoadedSetter(true);
					}
				}
			}
			if (loadActionTask != null)
			{
				await loadActionTask.WithoutContextCapture();
				return true;
			}
			return false;
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
