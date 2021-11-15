using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CustomResources.Utils.Concepts.DataStructures;
using NUnit.Framework;

namespace ApplicationResourcesTests.GeneralTests
{
	public class TaskQueueTests : GeneralTestBase
	{
		[Test]
		public async Task TestTaskQueue()
		{
			const int startAsyncLocalValue = 1;
			const int numCompletingTasks = 20;
			TimeSpan delay = TimeSpan.FromMilliseconds(100);
			CancellationTokenSource queueTokenSource = new CancellationTokenSource();
			CancellationTokenSource taskTokenSource = new CancellationTokenSource();
			var latestAsyncLocalValue = startAsyncLocalValue;
			var asyncLocal = new AsyncLocal<int> { Value = startAsyncLocalValue };

			var finishTimes = new InternalConcurrentDictionary<int, DateTime>();

			async Task<int> DoWork(int i, CancellationToken cancellationToken)
			{
				if (i < -1)
					throw new ArgumentException("expectedException");
				if (i == -1)
				{
					taskTokenSource.Cancel();
					taskTokenSource = new CancellationTokenSource();
				}
				cancellationToken.ThrowIfCancellationRequested();
				Assert.AreEqual(startAsyncLocalValue, asyncLocal.Value);
				await Task.Delay(delay, cancellationToken);
				var currTime = DateTime.Now;
				Assert.IsTrue(finishTimes.TryAdd(i, currTime));
				cancellationToken.ThrowIfCancellationRequested();
				Assert.AreEqual(startAsyncLocalValue, asyncLocal.Value);
				var incrementedValue = Interlocked.Increment(ref latestAsyncLocalValue);
				asyncLocal.Value = incrementedValue;
				Assert.AreEqual(incrementedValue, asyncLocal.Value);
				if (i != 1 && i <= numCompletingTasks + 1)
				{
					Assert.IsTrue(finishTimes.TryGetValue(i - 1, out var prevFinishTime));
					Assert.GreaterOrEqual(currTime + TimeSpan.FromMilliseconds(15), prevFinishTime + delay);
				}
				return 0;
			}

			using var queue = new CallbackTaskQueue<int, int>(DoWork, queueTokenSource.Token);

			var startTime = DateTime.Now;
			var tasks = Enumerable.Range(1, numCompletingTasks).Select(i => queue.Schedule(i)).ToList();
			foreach (var task in tasks)
				Assert.AreEqual(0, await task);

			Assert.ThrowsAsync<ArgumentException>(async () => await queue.Schedule(-2));
			Assert.ThrowsAsync<TaskCanceledException>(async () => await queue.Schedule(-1, taskTokenSource.Token));
			taskTokenSource.Cancel();
			Assert.ThrowsAsync<OperationCanceledException>(async () => await queue.Schedule(0, taskTokenSource.Token));
			queueTokenSource.Cancel();
			var lastCanceledTask = queue.Schedule(0);
			Assert.ThrowsAsync<InvalidOperationException>(async () => await lastCanceledTask);
			Assert.Pass();

		}

	}
}
