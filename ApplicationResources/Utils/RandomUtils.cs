using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using ApplicationResources.Setup;
using CustomResources.Utils.Concepts.DataStructures;
using CustomResources.Utils.Extensions;

namespace ApplicationResources.Utils
{
	public static class RandomUtils
	{
		public static Random NewGenerator(out int seedUsed)
		{
			seedUsed = _msInternalSeedGenerator.Value();
			return new Random(seedUsed);
		}

		private static readonly Lazy<Func<int>> _msInternalSeedGenerator = new Lazy<Func<int>>(Expression.Lambda<Func<int>>(Expression.Call(null,
			typeof(Random).GetMethod("GenerateSeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)))
			.Compile());
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

	public static class RandomExtensions
	{
		public static bool NextBool(this Random generator) => generator.Next(2) > 0;

		public static T[] RandomShuffle<T>(this IEnumerable<T> sequence) => sequence.RandomShuffle(ThreadSafeRandom.Generator);

		public static V Sample<V>(this ProbabilityDistribution<V> distribution) => distribution.Sample(ThreadSafeRandom.Generator);
	}

}
