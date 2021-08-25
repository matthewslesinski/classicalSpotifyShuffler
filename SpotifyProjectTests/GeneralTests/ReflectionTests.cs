using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NUnit.Framework;
using SpotifyProject;
using SpotifyProject.Utils.GeneralUtils;
using SpotifyProject.Utils.Concepts;
using System.Linq;
using SpotifyProject.Utils.Extensions;
using SpotifyProject.Utils.Algorithms;

namespace SpotifyProjectTests.GeneralTests
{
	public class ReflectionTests : GeneralTestBase
	{

		[Test]
		public void TestDynamicBenchmarking()
		{
			var methodName = "TestFunc";
			var dynamicType = new Benchmarker().CreateBenchmarkableType(methodName, () => true);
			var methodInfo = dynamicType.GetMethod(methodName);
			var obj = Activator.CreateInstance(dynamicType);
			var output = methodInfo.Invoke(obj, Type.EmptyTypes);
			Assert.IsTrue((bool)output);
		}

		[Test]
		public void TestReflectionUtilsWorks()
		{
			var mock = new MockClass(0);
			Assert.AreEqual(mock.TestProperty2, ReflectionUtilsOld.GetPropertyByName<int>(mock, nameof(MockClass.TestProperty2)));
		}

		[Test]
		public void TestReflectionUtilsCaching()
		{
			var func1 = ReflectionUtils<MockClass>.RetrieveGetterByPropertyName<string>(nameof(MockClass.TestProperty1));
			var func2 = ReflectionUtils<MockClass>.RetrieveGetterByPropertyName<string>(nameof(MockClass.TestProperty1));
			Assert.AreSame(func1, func2);
		}

		[TestCase(0)]
		[TestCase(1)]
		public void TestAllMemberInfoHaveDifferentTokens(int testCaseIndex)
		{
			var testTypes = new[] { typeof(BenchmarkPropertyGettersBenchmarkSetup), typeof(MockClass) };
			var type = testTypes[testCaseIndex];

			var infos = type.GetProperties().Concat<MemberInfo>(type.GetConstructors()).Concat(type.GetMethods()).Append(type);
			var mapping = infos.ToDictionary(info => info.Name, info => info.MetadataToken);
			var maxName = mapping.Keys.Select(name => name.Length).Max();
			Logger.Information($"MetadataTokens for type {type.Name}:" + string.Concat(mapping.Select(kvp => $"\n\t{kvp.Key}{new string(' ', maxName + 2 - kvp.Key.Length)}||  {kvp.Value}")));
			CollectionAssert.AllItemsAreUnique(mapping.Values);
		}


		[Test]
		public void BenchmarkPropertyGetters()
		{
			new Benchmarker().RunBenchmark<BenchmarkPropertyGettersBenchmarkSetup>(nameof(BenchmarkPropertyGetters));
		}

		[Test]
		public void ManuallyBenchmarkPropertyGetters()
		{
			var instance = new BenchmarkPropertyGettersBenchmarkSetup();
			new Benchmarker().RunManualBenchmark(nameof(ManuallyBenchmarkPropertyGetters), 5, 1000, 20, instance);
		}

		[SimpleJob(RuntimeMoniker.Net50, invocationCount: 5000)]
		public class BenchmarkPropertyGettersBenchmarkSetup
		{
			private readonly static Func<object, string, object[]> _MockClassPropertyGetter = ReflectionUtilsOld.TryRetrievePropertyGetter(typeof(MockClass));
			private readonly MockClass mock = new MockClass(0);

			[Params(50)]
			public int ArrLength { get; set; } = 100;
			private MockClass[] _mocks;

			public BenchmarkPropertyGettersBenchmarkSetup()
			{
				_mocks = Enumerable.Range(0, ArrLength).Select(i => new MockClass(i)).OrderBy(m => 1234567 ^ m.TestProperty2).ToArray();
			}

			[IterationSetup]
			public void RunSetup() {
				RunReflectionUtilsGetter();
				_mocks = Enumerable.Range(0, ArrLength).Select(i => new MockClass(i)).OrderBy(m => 1234567 ^ m.TestProperty2).ToArray();
			}

			[Benchmark(Baseline = true)]
			public object RunBaseline() => mock.TestProperty1;
			[Benchmark]
			public object RunArrayRetrieval() => mock.GetTestProperty2();
			[Benchmark]
			public object RunDictionaryRetrieval() => mock.GetTestProperty1();
			[Benchmark]
			public string RunCastedDictionaryRetrieval() => (string)mock.GetTestProperty1();
			[Benchmark]
			public object RunReflectionUtilsGetterRetriever() => ReflectionUtilsOld.TryRetrievePropertyGetter(typeof(MockClass));
			[Benchmark]
			public object RunReflectionUtilsHelper() => ReflectionUtilsOld.TryGetPropertyByName<MockClass, object>(mock, nameof(MockClass.TestProperty1), out var val) ? val : null;
			[Benchmark]
			public object RunReflectionUtilsFoundGetter() => _MockClassPropertyGetter(mock, nameof(MockClass.TestProperty1))[1];
			[Benchmark]
			public object RunReflectionUtilsGetter() => mock.TryGetPropertyByName<object>(nameof(MockClass.TestProperty1), out var val) ? val : null;
			[Benchmark]
			public object RunReflectionUtils2getter() => ReflectionUtilsOld.GetPropertyByName<object>(mock, nameof(MockClass.TestProperty1));
			[Benchmark]
			public object RunGenericReflectionUtilsGetter() => ReflectionUtils<MockClass>.RetrieveGetterByPropertyName<object>(nameof(MockClass.TestProperty1))(mock);
			[Benchmark]
			public object RunStraightReflection() => typeof(MockClass).GetProperty(nameof(MockClass.TestProperty1)).GetMethod.Invoke(mock, new object[] { });

			[Benchmark]
			public MockClass[] RunLISWithSimpleGetter() => RunTestSuite(m => m.TestProperty2);
			[Benchmark]
			public MockClass[] RunLISWithSlightlyOptimizedReflectionUtils2Getter() => RunTestSuite(ReflectionUtils.RetrieveGetterByPropertyName<MockClass, int>(nameof(MockClass.TestProperty2)));
			[Benchmark]
			public MockClass[] RunLISWithSlightlyOptimizedParameterizedReflectionUtilsGetter() => RunTestSuite(ReflectionUtils<MockClass>.RetrieveGetterByPropertyName<int>(nameof(MockClass.TestProperty2)));
			[Benchmark]
			public MockClass[] RunLISWithParameterizedReflectionUtilsGetter() => RunTestSuite(m => ReflectionUtils<MockClass>.RetrieveGetterByPropertyName<int>(nameof(MockClass.TestProperty2))(m));
			[Benchmark]
			public MockClass[] RunLISWithReflectionUtilsGetter() => RunTestSuite(m => m.TryGetPropertyByName<int>(nameof(MockClass.TestProperty2), out var val) ? val : -1);
			[Benchmark]
			public MockClass[] RunLISWithReflectionUtils2Getter() => RunTestSuite(m => ReflectionUtilsOld.GetPropertyByName<int>(m, nameof(MockClass.TestProperty2)));
			[Benchmark]
			public MockClass[] RunLISWithReflectionGetter() => RunTestSuite(m => (int) typeof(MockClass).GetProperty(nameof(MockClass.TestProperty2)).GetGetMethod().Invoke(m, Array.Empty<object>()));

			private MockClass[] RunTestSuite(Func<MockClass, int> order)
			{
				return LCS.GetLISIndices(_mocks, ComparerUtils.ComparingBy<MockClass>(mock => order(mock))).Select(i => _mocks[i]).ToArray();
			}
		}

		public class MockClass
		{
			public MockClass(int testProperty2)
			{
				TestProperty1 = nameof(TestProperty1);
				TestProperty2 = testProperty2;
				_mockDictionary = new Dictionary<string, string> { { nameof(TestProperty1), TestProperty1 } };
				_mockArray = new[] { TestProperty2 };
			}
			public string TestProperty1 { get; }
			public int TestProperty2 { get; }

			public static string TestMethod1(object o) => o.ToString();
			public static string TestMethod2<T>(T o) => o.ToString();

			public object GetTestProperty1() => _mockDictionary[nameof(TestProperty1)];
			public int GetTestProperty2() => _mockArray[0];
			private readonly Dictionary<string, string> _mockDictionary;
			private readonly int[] _mockArray;
		}
	}
}
