using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using ApplicationResources.Logging;
using ApplicationResources.Utils;
using NUnit.Framework;
using CustomResources.Utils.Concepts;
using CustomResources.Utils.Concepts.DataStructures;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;

namespace ApplicationResourcesTests.GeneralTests
{
	public class DataStructureTests : GeneralTestBase
	{
		[Test]
		public void TestSortedSetExtensions()
		{
			var sortedSet = new SortedSet<double>(new[] { 1d, 2d, 3d, 4d });

			// Query with .5
			Assert.IsNull(sortedSet.TryGetFloor(.5d, out var result) ? result : null);
			Assert.IsNull(sortedSet.TryGetPrevious(.5d, out result) ? result : null);
			Assert.AreEqual(1d, sortedSet.TryGetNext(.5d, out result) ? result : null, .01);
			Assert.AreEqual(1d, sortedSet.TryGetCeiling(.5d, out result) ? result : null, .01);

			// Query with 5
			Assert.IsNull(sortedSet.TryGetCeiling(5d, out result) ? result : null);
			Assert.IsNull(sortedSet.TryGetNext(5d, out result) ? result : null);
			Assert.AreEqual(4d, sortedSet.TryGetPrevious(5d, out result) ? result : null, .01);
			Assert.AreEqual(4d, sortedSet.TryGetFloor(5d, out result) ? result : null, .01);

			// Query with 2.5
			Assert.AreEqual(3d, sortedSet.TryGetCeiling(2.5d, out result) ? result : null, .01);
			Assert.AreEqual(3d, sortedSet.TryGetNext(2.5d, out result) ? result : null, .01);
			Assert.AreEqual(2d, sortedSet.TryGetPrevious(2.5d, out result) ? result : null, .01);
			Assert.AreEqual(2d, sortedSet.TryGetFloor(2.5d, out result) ? result : null, .01);

			// Query with 2
			Assert.AreEqual(2d, sortedSet.TryGetCeiling(2d, out result) ? result : null, .01);
			Assert.AreEqual(3d, sortedSet.TryGetNext(2d, out result) ? result : null, .01);
			Assert.AreEqual(1d, sortedSet.TryGetPrevious(2d, out result) ? result : null, .01);
			Assert.AreEqual(2d, sortedSet.TryGetFloor(2d, out result) ? result : null, .01);
		}

		[TestCase(300, 0, 200, 0, 0)]
		[TestCase(300, 256, 0, 300, 0)]
		[TestCase(1000, 100, 100, 100, 0)]
		[TestCase(10000, 0, 200, 100, 10)]
		[TestCase(10000, 0, 100, 100, 10)]
		[TestCase(10000, 128, 100, 100, 1)]
		public void TestEnumSet(int numTrials, int initialElements, int addLikelihood, int removeLikelihood, int enumerableArgMethodLikelihood)
		{
			var testGenerator = RandomUtils.NewGenerator(out var seed);
			Logger.Information($"Test {nameof(DataStructureTests)}.{nameof(TestEnumSet)} is using a random generator with seed {seed}");
			var maxElement = 256;
			var possibleElements = Enumerable.Range(0, maxElement).RandomShuffle(testGenerator).ToList();
			var equalityMeasure = new SimpleEqualityComparer<ISet<int>>((set1, set2) => set1.SetEquals(set2) && set2.SetEquals(set1), null);

			(EnumSet<int> testStructure, HashSet<int> referenceStructure) CreateDataStructures(Random generator)
			{
				var elements = possibleElements.GetRange(0, initialElements);
				return (elements.ToEnumSet(), elements.ToHashSet());
			}

			string SetToString(ISet<int> set) => "{" + string.Join(", ", set) + "}";

			int GetRandomElement(Random generator, ICollection<int> set) => set.Take(generator.Next(set.Count)).FirstOrDefault();

			ISet<int> CreateRandomSet(Random generator, int minSize, int maxSize, IEnumerable<int> prefixElements = null) =>
				(generator.NextBool() ? (Func<IEnumerable<int>, ISet<int>>) EnumSets.ToEnumSet : Enumerable.ToHashSet)
				((prefixElements ?? Array.Empty<int>()).Concat(possibleElements.RandomShuffle(generator)).Take(generator.Next(minSize, maxSize + 1)));

			TestDataStructureFunctionality(
				numTrials,
				testGenerator,
				equalityMeasure,
				CreateDataStructures,
				SetToString,
				SetToString,
				new MethodTestConfig<ISet<int>, bool>(addLikelihood, nameof(ISet<int>.Add), null, (generator, _) => generator.Next(maxElement)),
				new MethodTestConfig<ICollection<int>, bool>(removeLikelihood >> 1, nameof(ISet<int>.Remove), null, (generator, _) => generator.Next(maxElement)),
				new MethodTestConfig<ICollection<int>, bool>("RemovePresentItem", (removeLikelihood >> 1) + (removeLikelihood & 1), nameof(ISet<int>.Remove), null, (generator, set) => GetRandomElement(generator, set)),
				new MethodTestConfig<ICollection<int>, bool>(100, nameof(ISet<int>.Contains), null, (generator, _) => generator.Next(maxElement)),
				new MethodTestConfig<ICollection<int>, bool>("ContainsPresentItem", 20, nameof(ISet<int>.Contains), null, (generator, set) => GetRandomElement(generator, set)),
				new MethodTestConfig<ISet<int>, int>("Count", 20, set => set.Count),
				new MethodTestConfig<ICollection<int>, ICollection<int>>(1, nameof(ISet<int>.Clear), (set1, set2) => set1.Count == set2.Count),
				new MethodTestConfig<ISet<int>, int[]>(100, typeof(Enumerable).GetMethod(nameof(Enumerable.ToArray), BindingFlags.Static | BindingFlags.Public).MakeGenericMethod(typeof(int)), (arr1, arr2) => arr1.ContainsSameElements(arr2)),
				new MethodTestConfig<ISet<int>, ISet<int>>(enumerableArgMethodLikelihood, nameof(ISet<int>.IntersectWith), equalityMeasure.Equals, (generator, set) => CreateRandomSet(generator, 0, set.Count << 1)),
				new MethodTestConfig<ISet<int>, ISet<int>>(enumerableArgMethodLikelihood, nameof(ISet<int>.UnionWith), equalityMeasure.Equals, (generator, set) => CreateRandomSet(generator, 0, set.Count << 1)),
				new MethodTestConfig<ISet<int>, ISet<int>>(enumerableArgMethodLikelihood, nameof(ISet<int>.SymmetricExceptWith), equalityMeasure.Equals, (generator, set) => CreateRandomSet(generator, 0, set.Count << 1)),
				new MethodTestConfig<ISet<int>, ISet<int>>(enumerableArgMethodLikelihood, nameof(ISet<int>.ExceptWith), equalityMeasure.Equals, (generator, set) => CreateRandomSet(generator, 0, set.Count << 1)),
				new MethodTestConfig<ISet<int>, bool>(enumerableArgMethodLikelihood, nameof(ISet<int>.Overlaps), null, (generator, set) => CreateRandomSet(generator, 0, set.Count << 1)),
				new MethodTestConfig<ISet<int>, bool>("OverlapsWithNone", enumerableArgMethodLikelihood, nameof(ISet<int>.Overlaps), null, (generator, set) => CreateRandomSet(generator, 0, 0)),
				new MethodTestConfig<ISet<int>, bool>("OverlapsWithSome", enumerableArgMethodLikelihood, nameof(ISet<int>.Overlaps), null, (generator, set) => CreateRandomSet(generator, 0, 1, set.Take(1))),
				new MethodTestConfig<ISet<int>, bool>(enumerableArgMethodLikelihood, nameof(ISet<int>.SetEquals), null, (generator, set) => CreateRandomSet(generator, 0, set.Count << 1)),
				new MethodTestConfig<ISet<int>, bool>("SetEqualsSet", enumerableArgMethodLikelihood, nameof(ISet<int>.SetEquals), null, (generator, set) => CreateRandomSet(generator, set.Count, set.Count, set)),
				new MethodTestConfig<ISet<int>, bool>(enumerableArgMethodLikelihood, nameof(ISet<int>.IsSubsetOf), null, (generator, set) => CreateRandomSet(generator, 0, set.Count << 1, generator.NextBool() ? set : Array.Empty<int>())),
				new MethodTestConfig<ISet<int>, bool>(enumerableArgMethodLikelihood, nameof(ISet<int>.IsProperSubsetOf), null, (generator, set) => CreateRandomSet(generator, 0, set.Count << 1, generator.NextBool() ? set : Array.Empty<int>())),
				new MethodTestConfig<ISet<int>, bool>(enumerableArgMethodLikelihood, nameof(ISet<int>.IsSupersetOf), null, (generator, set) => CreateRandomSet(generator, 0, set.Count << 1, generator.NextBool() ? set : Array.Empty<int>())),
				new MethodTestConfig<ISet<int>, bool>(enumerableArgMethodLikelihood, nameof(ISet<int>.IsProperSupersetOf), null, (generator, set) => CreateRandomSet(generator, 0, set.Count << 1, generator.NextBool() ? set : Array.Empty<int>()))
			);
		}

		private static void TestDataStructureFunctionality<InterfaceT, TestStructureT, ReferenceStructureT>(int numRuns, Random generator,
			IEqualityComparer<InterfaceT> equalityMeasure, Func<Random, (TestStructureT testStructure, ReferenceStructureT referenceStructure)> dataStructureCreator,
			Func<TestStructureT, string> testStructureToString, Func<ReferenceStructureT, string> referenceStructureToString,
			params IMethodTestConfig<InterfaceT>[] testMethods)
				where TestStructureT : InterfaceT where ReferenceStructureT : InterfaceT
		{
			var currentTestName = "Creating data structures";
			var testStructureString = "Not created yet";
			var referenceStructureString = "Not created yet";
			var callDistribution = ProbabilityDistribution<(string testName, MethodEvaluation<InterfaceT> test)>.From(testMethods
				.Select(testMethod => ((testMethod.Name, (MethodEvaluation<InterfaceT>) testMethod.RunTest), testMethod.RelativeLikelihood)));
			try
			{
				var (testStructure, referenceStructure) = dataStructureCreator(generator);

				var possibleTests = callDistribution.ToList();
				var tests = possibleTests.Select(tup => tup.result)
					.Concat(Enumerable.Range(0, numRuns - possibleTests.Count).Select(_ => callDistribution.Sample(generator)))
					.RandomShuffle(generator);

				foreach (var (testName, test) in tests)
				{
					currentTestName = testName;
					testStructureString = testStructureToString(testStructure);
					referenceStructureString = referenceStructureToString(referenceStructure);
					var testPassed = test(generator, testStructure, referenceStructure, out var testResult, out var referenceResult, out var args);
					var newTestStructureString = testStructureToString(testStructure);
					var newReferenceStructureString = referenceStructureToString(referenceStructure);
					Assert.True(testPassed,
						$"The returned results from testing the {currentTestName} method on the structures were different. " +
						$"The returned results were {testResult} and {referenceResult}. " +
						$"The two data structures were {testStructureString} and {referenceStructureString} before the method, " +
						$"and {newTestStructureString} and {newReferenceStructureString} after the method. " +
						$"The supplied input to the test was [{string.Join(", ", args)}]");

					Assert.True(equalityMeasure.Equals(testStructure, referenceStructure),
						$"The data structures were modified during test {currentTestName} in ways that made them no longer equal. " +
						$"Before the operation, they were {testStructureString} and {referenceStructureString}, " +
						$"and after they were {newTestStructureString} and {newReferenceStructureString}. " +
						$"The supplied input to the test was [{string.Join(", ", args)}]");
				}
			}
			catch (Exception e)
			{
				Assert.Fail($"An exception occurred while evaluating test {currentTestName}. The current data structures are {testStructureString} and {referenceStructureString}. {e}");
			}
		}

		private static bool AreExceptionsTheSame(Exception e1, Exception e2) => e1.GetType() == e2.GetType();

		private static readonly object _voidSentinel = new object();
		private delegate bool MethodEvaluation<in DataStructureT>(Random generator, DataStructureT testStructure, DataStructureT referenceStructure, out object testResult, out object referenceResult, out object[] args);

		private interface IMethodTestConfig<in InterfaceT>
		{
			bool RunTest(Random generator, InterfaceT testStructure, InterfaceT referenceStructure, out object testResult, out object referenceResult, out object[] args);
			string Name { get; }
			int RelativeLikelihood { get; }
		}

		private class MethodTestConfig<InterfaceT, ResultT> : IMethodTestConfig<InterfaceT>
		{
			private readonly Func<ResultT, ResultT, bool> _equalityMeasure;
			private readonly Func<Random, InterfaceT, object[], ResultT> _testMethod;
			private readonly IEnumerable<Func<Random, InterfaceT, object>> _argCreators;

			internal MethodTestConfig(string testName, int relativeLikelihood, Func<InterfaceT, ResultT> testMethod, Func<ResultT, ResultT, bool> equalityMeasure = null)
			{
				_equalityMeasure = equalityMeasure ?? EqualityComparer<ResultT>.Default.Equals;
				_testMethod = (_, structure, _) => testMethod(structure);
				_argCreators = Array.Empty<Func<Random, InterfaceT, object>>();
				Name = testName;
				RelativeLikelihood = relativeLikelihood;
			}

			internal MethodTestConfig(int relativeLikelihood, string methodName, Func<ResultT, ResultT, bool> equalityMeasure, params Func<Random, InterfaceT, object>[] argCreators)
				: this(relativeLikelihood, typeof(InterfaceT).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy), equalityMeasure, argCreators) { }

			internal MethodTestConfig(string testName, int relativeLikelihood, string methodName, Func<ResultT, ResultT, bool> equalityMeasure, params Func<Random, InterfaceT, object>[] argCreators)
				: this(testName, relativeLikelihood, typeof(InterfaceT).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy), equalityMeasure, argCreators) { }

			internal MethodTestConfig(int relativeLikelihood, MethodInfo methodInfo, Func<ResultT, ResultT, bool> equalityMeasure, params Func<Random, InterfaceT, object>[] argCreators)
				: this(methodInfo?.Name, relativeLikelihood, methodInfo, equalityMeasure, argCreators) { }

			internal MethodTestConfig(string name, int relativeLikelihood, MethodInfo methodInfo, Func<ResultT, ResultT, bool> equalityMeasure, params Func<Random, InterfaceT, object>[] argCreators)
			{
				if (methodInfo == null)
					throw new ArgumentException($"No method exists to use for test {name}");

				if (methodInfo.ReturnType == typeof(void) && typeof(InterfaceT) != typeof(ResultT))
					throw new ArgumentException($"The test method must return a value");

				Name = name;
				RelativeLikelihood = relativeLikelihood;
				_argCreators = argCreators;
				_equalityMeasure = equalityMeasure ?? EqualityComparer<ResultT>.Default.Equals;


				var isExtensionMethod = methodInfo.IsDefined(typeof(ExtensionAttribute));
				var randomInput = Expression.Parameter(typeof(Random));
				var structureInput = Expression.Parameter(typeof(InterfaceT));
				var argsInput = Expression.Parameter(typeof(object[]));

				var methodParams = methodInfo.GetParameters();
				var argIndices = _argCreators.Select((_, i) => Expression.Constant(i));
				var callInputs = (isExtensionMethod ? new [] { structureInput } : Array.Empty<ParameterExpression>())
					.Concat<Expression>(argIndices.Select(inputIndexExpr => Expression.ArrayIndex(argsInput, inputIndexExpr)))
					.Zip(methodParams, (inputParam, methodParam) => inputParam.Cast(methodParam.ParameterType));
				var call = isExtensionMethod ? Expression.Call(methodInfo, callInputs) : Expression.Call(structureInput, methodInfo, callInputs);
				var block = methodInfo.ReturnType == typeof(void) ? Expression.Block(call, structureInput) : (Expression) Expression.Convert(call, typeof(ResultT));
				_testMethod = Expression.Lambda<Func<Random, InterfaceT, object[], ResultT>>(block, randomInput, structureInput, argsInput).Compile();
			}

			public string Name { get; }
			public int RelativeLikelihood { get; }

			public bool RunTest(Random generator, InterfaceT testStructure, InterfaceT referenceStructure, out object testResult, out object referenceResult, out object[] args)
			{
				args = _argCreators.Select(argCreator => argCreator(generator, referenceStructure)).ToArray();
				return EvaluateMethod(generator, testStructure, referenceStructure, args, out testResult, out referenceResult);
			}

			private bool EvaluateMethod(Random generator, InterfaceT testStructure, InterfaceT referenceStructure, object[] args, out object testResult, out object referenceResult)
			{
				void RunTest(InterfaceT dataStructure, out bool exceptionThrown, out ResultT resultValue, out Exception resultException)
				{
					try
					{
						resultValue = _testMethod(generator, dataStructure, args);
						exceptionThrown = false;
						resultException = default;
					}
					catch (Exception e)
					{
						exceptionThrown = true;
						resultException = e;
						resultValue = default;
					}
				}
				RunTest(testStructure, out var testStructureThrew, out var testResultValue, out var testResultException);
				RunTest(referenceStructure, out var referenceStructureThrew, out var referenceResultValue, out var referenceResultException);
				testResult = testStructureThrew ? testResultException : testResultValue;
				referenceResult = referenceStructureThrew ? referenceResultException : referenceResultValue;
				return (testStructureThrew && referenceStructureThrew)
					? AreExceptionsTheSame(testResultException, referenceResultException)
					: (!testStructureThrew && !referenceStructureThrew && _equalityMeasure(testResultValue, referenceResultValue));
			}
		}
	}
}
