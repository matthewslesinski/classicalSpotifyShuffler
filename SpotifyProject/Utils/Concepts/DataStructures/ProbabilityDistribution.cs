using System;
using System.Collections.Generic;
using System.Linq;
using SpotifyProject.Utils.Extensions;
using SpotifyProject.Utils.GeneralUtils;

namespace SpotifyProject.Utils.Concepts.DataStructures
{
	public class ProbabilityDistribution<V> : IEnumerable<(V result, decimal probability)>
	{
		private readonly SortedList<decimal, V> _distribution;

		private ProbabilityDistribution(IEnumerable<(decimal probabilityTierLowerBound, V value)> distribution)
		{
			foreach(var (probabilityTierLowerBound, _) in distribution)
			{
				if (_decimalComparerWithTolerance.LessThan(probabilityTierLowerBound, 0m) || _decimalComparerWithTolerance.GreaterThan(probabilityTierLowerBound, 1m))
					throw new ArgumentException($"A probability must be between 0 and 1, but received {probabilityTierLowerBound}");
			}

			_distribution = new SortedList<decimal, V>(distribution.ToDictionary(GeneralExtensions.GetFirst, GeneralExtensions.GetSecond), _decimalComparerWithTolerance);
		}

		public V Sample(Random generator = null)
		{
			var sampledDecimal = Convert.ToDecimal((generator ?? ThreadSafeRandom.Generator).NextDouble());
			if (_distribution.TryGetCeiling(sampledDecimal, out var foundDecimal))
				return _distribution[foundDecimal];
			else
				throw new Exception("Either the distribution has probabilities not between 0 and 1 or tolerance needs to be increased. " +
					$"The sampled value was {sampledDecimal} and the bounds in the distribution were [{string.Join(", ", _distribution.Keys)}].");
		}

		public IEnumerator<(V result, decimal probability)> GetEnumerator()
		{
			var prevUpperBound = 0m;
			foreach(var kvp in _distribution)
			{
				var upperBound = kvp.Key;
				yield return (kvp.Value, upperBound - prevUpperBound);
				prevUpperBound = upperBound;
			}
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

		public static ProbabilityDistribution<V> From(IEnumerable<KeyValuePair<V, int>> values) => From(values?.Select(kvp => (kvp.Key, (decimal)kvp.Value)));
		public static ProbabilityDistribution<V> From(IEnumerable<KeyValuePair<V, double>> values) => From(values?.Select(kvp => (kvp.Key, (decimal)kvp.Value)));
		public static ProbabilityDistribution<V> From(IEnumerable<KeyValuePair<V, decimal>> values) => From(values?.Select(kvp => (kvp.Key, kvp.Value)));
		public static ProbabilityDistribution<V> From(IEnumerable<(V, int)> values) => From(values?.Select(tup => (tup.Item1, (decimal)tup.Item2)));
		public static ProbabilityDistribution<V> From(IEnumerable<(V, double)> values) => From(values?.Select(tup => (tup.Item1, (decimal) tup.Item2)));
		public static ProbabilityDistribution<V> From(IEnumerable<(V, decimal)> values)
		{
			if (values == null || !values.Any() || values.All(entry => _decimalComparerWithTolerance.Equals(entry.Item2, 0m)))
				throw new ArgumentException($"You can't have a probability distribution with no possible events");
			var total = values.Sum(tup => tup.Item2);
			var probabilities = values.Select(tup => (tup.Item2 / total, tup.Item1));
			return new ProbabilityDistribution<V>(ToDistribution(probabilities));
		}

		private static IEnumerable<(decimal, V)> ToDistribution(IEnumerable<(decimal, V)> probabilities)
		{
			var upperBound = 0m;
			foreach (var (probability, value) in probabilities)
			{
				if (_decimalComparerWithTolerance.GreaterThan(probability, 0m))
				{
					upperBound += probability;
					yield return (upperBound, value);
				}
			}
		}

		private static readonly IComparer<decimal> _decimalComparerWithTolerance = new ToleranceComparer<decimal>(.00000000000001m);
	}
}
