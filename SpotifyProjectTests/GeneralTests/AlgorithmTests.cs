using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SpotifyProject.Utils.Algorithms;
using SpotifyProject.Utils.Extensions;

namespace SpotifyProjectTests.GeneralTests
{
	public class AlgorithmTests : GeneralTestBase
	{

		[TestCase(new int[] { 1, 2, 3, 4, 5 }, ExpectedResult = new int[] { 1, 2, 3, 4, 5 })]
		[TestCase(new int[] { }, ExpectedResult = new int[] { })]
		[TestCase(new int[] { 5, 4, 3, 2, 1 }, ExpectedResult = new int[] { 1 })]
		[TestCase(new int[] { 1, 2, 5, 3, 4 }, ExpectedResult = new int[] { 1, 2, 3, 4 })]
		[TestCase(new int[] { 4, 3, 2, 1, 5 }, ExpectedResult = new int[] { 1, 5 })]
		public int[] TestLISInts(int[] sequence)
		{
			var lisIndices = LCS.GetLISIndices(sequence);
			var lis = lisIndices.Select(sequence.Get).ToArray();
			if (sequence.Any()) {
				Enumerable.Range(0, lis.Length - 1).Each(i => Assert.Less(lis[i], lis[i + 1], $"The elements at indices {i} and {i + 1} are not increasing"));
				Assert.That(lis, Is.Not.Empty);
				using (var lisEnumerator = lis.ToList().GetEnumerator()) {
					lisEnumerator.MoveNext();
					int? next = lisEnumerator.Current;
					int? prev = null;
					foreach (var element in sequence)
					{
						if (element == next)
						{
							prev = next;
							next = lisEnumerator.MoveNext() ? lisEnumerator.Current : null;
						}
						else
						{
							if (prev == null)
								Assert.Greater(element, next.Value);
							else if (next == null)
								Assert.Less(element, prev.Value);
							else
								Assert.That(element, Is.GreaterThan(next.Value).Or.LessThan(prev.Value));
						}
					}
				}
			}
			return lis;
		}

		[TestCase(new int[] { 1, 2, 3, 4, 5 }, new int[] { 1, 2, 3, 4, 5 }, ExpectedResult = new int[] { 1, 2, 3, 4, 5 })]
		[TestCase(new int[] { 1, 2, 3, 4, 5 }, new int[] { 1, 2, 3, 4 }, ExpectedResult = new int[] { 1, 2, 3, 4 })]
		[TestCase(new int[] { 1, 2, 4, 5 }, new int[] { 1, 2, 3, 4, 5 }, ExpectedResult = new int[] { 1, 2, 4, 5 })]
		[TestCase(new int[] { 4, 5, 2, 1, 3 }, new int[] { 2, 4, 5, 3, 1 }, ExpectedResult = new int[] { 4, 5, 3 })]
		[TestCase(new int[] { 4, 5, 2, 3 }, new int[] { 3, 5, 1, 2 }, ExpectedResult = new int[] { 5, 2 })]
		public int[] TestLCSInts(int[] sequence1, int[] sequence2, IEqualityComparer<int> equalityComparer = null)
		{
			var lcsIndices = LCS.GetLCSIndices(sequence1, sequence2, equalityComparer);
			var sequence1LCS = lcsIndices.Select(GeneralExtensions.GetFirst).Select(sequence1.Get).ToArray();
			var sequence2LCS = lcsIndices.Select(GeneralExtensions.GetSecond).Select(sequence2.Get).ToArray();
			Assert.That(sequence1LCS, Is.EquivalentTo(sequence2LCS));
			return sequence1LCS;
		}
	}
}
