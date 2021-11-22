using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using CustomResources.Utils.Extensions;

namespace CustomResources.Utils.Concepts.DataStructures
{
	public abstract class TaskQueue<InputT, OutputT> : StandardDisposable, IDisposable
	{
		private readonly BlockingCollection<Node> _queue = new BlockingCollection<Node>();
		private readonly CancellationToken _workerCancellationToken;

		#pragma warning disable IDE0052 // Remove unread private members
		private readonly Task _workerTask;
		#pragma warning restore IDE0052 // Remove unread private members

		private int _isRunning = 1;

		public TaskQueue(CancellationToken cancellationToken = default)
		{
			_workerCancellationToken = cancellationToken;
			_workerCancellationToken.Register(StopRunning);
			_workerTask = Task.Run(Process, _workerCancellationToken);
		}

		public void StopRunning()
		{
			if (Interlocked.Exchange(ref _isRunning, 0) == 1)
			{
				_queue.CompleteAdding();
				while (_queue.TryTake(out var node))
					node.TaskCompleter.SetCanceled();
			}
		}

		protected override void DoDispose()
		{
			StopRunning();
			_queue.Dispose();
		}

		public async Task<OutputT> Schedule(InputT input, CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var taskCompleter = new TaskCompletionSource<OutputT>(TaskCreationOptions.RunContinuationsAsynchronously);
			var ec = ExecutionContext.Capture();
			var node = new Node(input, taskCompleter, ec, cancellationToken);
			if (!_queue.TryAdd(node))
			{
				await Task.Yield();
				_queue.Add(node, cancellationToken);
			}
			return await taskCompleter.Task;
		}

		private async Task Process()
		{
			foreach(var (input, taskCompleter, executionContext, taskCancellationToken) in _queue.GetConsumingEnumerable(_workerCancellationToken))
			{
				if (taskCancellationToken.IsCancellationRequested)
					taskCompleter.SetCanceled(taskCancellationToken);
				else if (_workerCancellationToken.IsCancellationRequested)
					taskCompleter.SetCanceled(_workerCancellationToken);
				else if (_alreadyDisposed == 1 || _isRunning == 0)
					taskCompleter.SetCanceled();
				else
				{
					try
					{
						Task<OutputT> handler = null;
						if (executionContext == null)
							handler = HandleTask(input, taskCancellationToken);
						else
						{
							ExecutionContext.Run(executionContext, _ =>
							{
								handler = HandleTask(input, taskCancellationToken);
							}, null);
						}

						var output = await handler.WithoutContextCapture();
						taskCompleter.SetResult(output);
					}
					catch (OperationCanceledException e) when (e.CancellationToken == taskCancellationToken)
					{
						taskCompleter.SetCanceled(taskCancellationToken);
					}
					catch (Exception e)
					{
						taskCompleter.SetException(e);
					}
				}
			}
		}

		protected abstract Task<OutputT> HandleTask(InputT input, CancellationToken taskCancellationToken);

		private record Node(InputT Input, TaskCompletionSource<OutputT> TaskCompleter, ExecutionContext ExecutionContext, CancellationToken TaskCancellationToken);
	}

	public class CallbackTaskQueue<InputT, OutputT> : TaskQueue<InputT, OutputT>
	{
		private readonly Func<InputT, CancellationToken, Task<OutputT>> _callback;
		public CallbackTaskQueue(Func<InputT, CancellationToken, Task<OutputT>> callback, CancellationToken cancellationToken = default) : base (cancellationToken)
		{
			_callback = callback;
		}

		protected override Task<OutputT> HandleTask(InputT input, CancellationToken taskCancellationToken)
		{
			return _callback(input, taskCancellationToken);
		}
	}
}
