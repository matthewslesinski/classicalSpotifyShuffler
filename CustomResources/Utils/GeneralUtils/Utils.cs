using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CustomResources.Utils.Concepts;
using CustomResources.Utils.Concepts.DataStructures;
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

		public static bool IsFirstRequest(ref int requestSwitch) => !Interlocked.Exchange(ref requestSwitch, true.AsInt()).AsBool();
		public static bool IsFirstRequest(ref Reference<bool> requestSwitch) => !Interlocked.Exchange(ref requestSwitch, true);
		public static bool IsFirstRequest(MutableReference<bool> requestSwitch) => !requestSwitch.AtomicExchange(true);

		// TODO Make these loading methods into a class
		public static bool LoadOnceBlocking(ref bool isLoaded, object loadLock, Action loadAction)
		{
			if (!isLoaded)
			{
				lock (loadLock)
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

		public static Task<bool> LoadOnceAsync(ref Reference<bool> isLoaded, Func<Task> loadAction)
		{
			if (IsFirstRequest(ref isLoaded))
			{
				return loadAction().ContinueWith(_ => true);
			}
			return Task.FromResult(false);
		}

		public static Task<bool> LoadOnceBlockingAsync(ref TaskCompletionSource<bool> completionSource, Func<Task> loadAction)
		{
			if (completionSource == null && Interlocked.CompareExchange(ref completionSource, new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously), null) == null)
			{
				var source = completionSource;

				async Task<bool> RunLoadAndCompleteTask()
				{
					try
					{
						await loadAction().WithoutContextCapture();
						source.SetResult(false);
						return true;
					}
					catch (OperationCanceledException e)
					{
						source.SetCanceled(e.CancellationToken);
						throw;
					}
					catch (Exception e)
					{
						source.SetException(e);
						throw;
					}
				}
				return RunLoadAndCompleteTask();
			}
			else
				return completionSource.Task;
		}

		public static async Task<bool> LoadOnceBlockingAsync(MutableReference<bool> isLoaded, AsyncLockProvider @lock, Func<Task> loadAction)
		{
			using (await @lock.AcquireToken().WithoutContextCapture())
			{
				if (!isLoaded)
				{
					await loadAction().WithoutContextCapture();
					isLoaded.Value = true;
					return true;
				}
				return false;
			}
		}

	}

	public static class Ensure
	{
		/// <summary>
		///   Checks an argument to ensure it isn't null.
		/// </summary>
		/// <param name = "value">The argument value to check</param>
		/// <param name = "name">The name of the argument</param>
		public static T ArgumentNotNull<T>(T value, string name)
		{
			if (value == null)
			{
				throw new ArgumentNullException(name);
			}

			return value;
		}

		public static T EnsureNotNull<T>(this T value, string name) => ArgumentNotNull(value, name);
	}
}
