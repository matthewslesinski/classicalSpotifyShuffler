using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using ApplicationResources.Utils;
using CustomResources.Utils.Concepts.DataStructures;
using NUnit.Framework;

namespace ApplicationResourcesTests.GeneralTests
{
	public class Experiments : GeneralTestBase
	{
		[Test]
		public void TestWhenExceptionsGetThrown()
		{
			Task DoSomething()
			{
				throw new Exception("hi");
			}
			async Task DoSomethingElse()
			{
				await Task.Yield();
				throw new Exception("hi");
			}

			Assert.ThrowsAsync<Exception>(() => DoSomething());
			Assert.ThrowsAsync<Exception>(async () => await DoSomething());
			var task = DoSomethingElse();
			Assert.ThrowsAsync<Exception>(async () => await task);
			Assert.ThrowsAsync<Exception>(async () => await task);
		}

		[Test]
		public void TestInternalConcurrentDictionaryOverrides()
		{
			var dict = new InternalConcurrentDictionary<int, int>();
			ConcurrentDictionary<int, int> asConcurrent = dict;
			IDictionary<int, int> asIDictionary = asConcurrent;
			IReadOnlyDictionary<int, int> asReadOnly = asConcurrent;

			Assert.AreEqual(typeof(KeyCollectionView<int, int, InternalConcurrentDictionary<int, int>>), dict.Keys.GetType());
			Assert.AreEqual(typeof(ReadOnlyCollection<int>), asConcurrent.Keys.GetType());
			Assert.AreEqual(typeof(KeyCollectionView<int, int, InternalConcurrentDictionary<int, int>>), asIDictionary.Keys.GetType());
			Assert.AreEqual(typeof(KeyCollectionView<int, int, InternalConcurrentDictionary<int, int>>), asReadOnly.Keys.GetType());
		}

		[Test]
		public async Task TestConfigureAwaitWithAsyncLocal()
		{
			var singleValue = 0;
			var checks = 0;
			var asyncLocal = new AsyncLocal<int> { Value = singleValue };
			Assert.AreEqual(singleValue, asyncLocal.Value);
			async Task RunDelayedTest(int order, int delay, int expectedChecks)
			{
				Assert.Less(singleValue, order);
				singleValue = order;
				asyncLocal.Value = order;
				await Task.Delay(delay).ConfigureAwait(false);
				Assert.AreEqual(expectedChecks, checks++);
				Assert.AreEqual(order, asyncLocal.Value);
			}
			var task1 = RunDelayedTest(1, 100, 1).ConfigureAwait(false);
			var task2 = RunDelayedTest(2, 50, 0).ConfigureAwait(false);
			await task1;
			await task2;
			Assert.AreEqual(2, checks);
			Assert.AreEqual(0, asyncLocal.Value);
		}

		[Test]
		public Task TestAsyncLocalUniqueToThreads()
		{
			var asyncVal = new AsyncLocal<int> { Value = -1 };
			Task RunTest(int uniqueValue)
			{
				return Task.Run(() =>
				{
					asyncVal.Value = uniqueValue;
					Task.WaitAll(Task.Delay(100).ContinueWith(completedTask => Assert.AreEqual(uniqueValue, asyncVal.Value)));
				});
					
			}
			return Task.WhenAll(RunTest(1), RunTest(2));
		}

		[Test]
		public void TestJsonWithInterfaces()
		{
			var instance = new TestClass1(new TestClass2());
			var str = instance.ToJsonString();
			var result = str.FromJsonString<TestClass1>();
			Assert.AreEqual(typeof(TestClass2), result.TestProp.GetType());
			Assert.AreEqual("hi", result.TestProp.Prop1);
			var instance2 = new TestClass1(new TestClass3());
			var str2 = instance2.ToJsonString();
			var result2 = str2.FromJsonString<TestClass1>();
			Assert.AreEqual(typeof(TestClass3), result2.TestProp.GetType());
			Assert.AreEqual("bye", result2.TestProp.Prop1);

		}

		private interface ITestInterface1
		{
			string Prop1 { get; }
		}

		private record TestClass1(ITestInterface1 TestProp);

		private record TestClass2(string Prop1 = "hi") : ITestInterface1;
		private record TestClass3(string Prop1 = "bye") : ITestInterface1;

	}
}
