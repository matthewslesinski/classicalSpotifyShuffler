using System;
using System.Collections.Generic;
using System.Linq;
using SpotifyProject.Utils.Extensions;
using SpotifyProject.Utils.Concepts;

namespace SpotifyProject.Utils
{
	public static class LCS
	{
		public static IEnumerable<(int sequence1Index, int sequence2Index)> GetLCSIndices<A, B>(A[] sequence1, B[] sequence2, Func<A, B> projector, IEqualityComparer<B> equalityComparer = null) =>
			GetLCSIndices(sequence1.Select(projector).ToArray(), sequence2, equalityComparer);

		public static IEnumerable<(int sequence1Index, int sequence2Index)> GetLCSIndices<T>(T[] sequence1, T[] sequence2, IEqualityComparer<T> equalityComparer = null)
		{
			equalityComparer ??= EqualityComparer<T>.Default;
			var firstElements = sequence1.ToHashSet(equalityComparer);
			var secondElements = sequence2.ToHashSet(equalityComparer);
			var firstSequenceIndices = sequence1.Enumerate().Where(pair => secondElements.Contains(pair.item)).Select(pair => pair.index).ToArray();
			var secondSequenceIndices = sequence2.Enumerate().Where(pair => firstElements.Contains(pair.item)).Select(pair => pair.index).ToArray();
			return GetLCSIndices(sequence1, firstSequenceIndices, sequence2, secondSequenceIndices, equalityComparer);
		}

		// Assumes the elements in the sequences pointed to by the index arrays are permutations of each other, and that the sequences don't have duplicates
		private static IEnumerable<(int sequence1Index, int sequence2Index)> GetLCSIndices<T>(T[] sequence1, int[] indicesToConsider1,
			T[] sequence2, int[] indicesToConsider2, IEqualityComparer<T> equalityComparer)
		{
			var secondSequenceIndexMap = indicesToConsider2.ToDictionary(sequence2.Get, equalityComparer);
			var ordering = ComparerUtils.ComparingBy<T>(item => secondSequenceIndexMap[item]);
			var firstIndices = GetLISIndices(sequence1, indicesToConsider1, ordering);
			var lcsElements = firstIndices.Select(sequence1.Get);
			var secondIndices = lcsElements.Select(secondSequenceIndexMap.Get);
			return firstIndices.Zip(secondIndices);
		}

		private static IEnumerable<int> GetLISIndices<T>(T[] sequence, int[] indicesToConsider, IComparer<T> comparer) =>
			GetLISIndices(indicesToConsider, ComparerUtils.ComparingBy<int, T>(sequence.Get, comparer))
				.Select(indicesToConsider.Get);

		public static IEnumerable<int> GetLISIndices<T>(T[] sequence, IComparer<T> comparer = null)
		{
			comparer ??= Comparer<T>.Default;
			IEnumerable<int> GetReversedIndices()
			{
				var sequenceElementOrdering = ComparerUtils.ComparingBy<int>(i => i >= 0).ThenBy(ComparerUtils.ComparingBy<int, T>(sequence.Get, comparer));
				var n = sequence.Length;
				var endIndices = new List<int> { -1 };
				var prevIndices = new int[n];
				for (var elementIndex = 0; elementIndex < n; elementIndex++)
				{
					var searchResult = endIndices.BinarySearch(elementIndex, sequenceElementOrdering);
					if (searchResult < 0)
					{
						var increasingSequenceLength = ~searchResult;
						endIndices.AddOrReplace(increasingSequenceLength, elementIndex);
						prevIndices[elementIndex] = endIndices[increasingSequenceLength - 1];
					}
				}

				for (var curr = endIndices.Last(); curr >= 0; curr = prevIndices[curr])
					yield return curr;
			}
			return GetReversedIndices().Reversed();
		}
	}
}
