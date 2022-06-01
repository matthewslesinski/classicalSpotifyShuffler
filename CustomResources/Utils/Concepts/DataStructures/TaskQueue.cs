using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using CustomResources.Utils.Extensions;

namespace CustomResources.Utils.Concepts.DataStructures
{
	public abstract class TaskQueue<InputT, OutputT> : TaskContainingDisposable
	{
		private readonly BlockingCollection<Node> _queue = new BlockingCollection<Node>();

		private int _isRunning = true.AsInt();

		public TaskQueue(CancellationToken cancellationToken = default) : base(cancellationToken)
		{
			StopToken.Register(StopRunning);
			Run(Process);
		}

		public void StopRunning()
		{
			if (Interlocked.Exchange(ref _isRunning, false.AsInt()).AsBool())
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
				await Task.Run(() => _queue.Add(node, cancellationToken), cancellationToken).WithoutContextCapture();
			return await taskCompleter.Task.WithoutContextCapture();
		}

		private async Task Process()
		{
			foreach (var (input, taskCompleter, executionContext, taskCancellationToken) in _queue.GetConsumingEnumerable(StopToken))
			{
				if (taskCancellationToken.IsCancellationRequested)
					taskCompleter.SetCanceled(taskCancellationToken);
				else if (StopToken.IsCancellationRequested)
					taskCompleter.SetCanceled(StopToken);
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

		private record struct Node(InputT Input, TaskCompletionSource<OutputT> TaskCompleter, ExecutionContext ExecutionContext, CancellationToken TaskCancellationToken);
	}

	public class CallbackTaskQueue<InputT, OutputT> : TaskQueue<InputT, OutputT>
	{
		private readonly Func<InputT, CancellationToken, Task<OutputT>> _callback;
		public CallbackTaskQueue(Func<InputT, CancellationToken, Task<OutputT>> callback, CancellationToken cancellationToken = default) : base(cancellationToken)
		{
			_callback = callback;
		}

		protected override Task<OutputT> HandleTask(InputT input, CancellationToken taskCancellationToken)
		{
			return _callback(input, taskCancellationToken);
		}
	}

	public class CallbackTaskQueue<InputT> : CallbackTaskQueue<InputT, object /*Represents a void return type*/>
	{
		public CallbackTaskQueue(Func<InputT, CancellationToken, Task> callback, CancellationToken cancellationToken = default)
			: base(async (input, taskToken) => { await callback(input, taskToken).WithoutContextCapture(); return null; }, cancellationToken)
		{ }

		public new Task Schedule(InputT input, CancellationToken cancellationToken = default) => base.Schedule(input, cancellationToken);

	}
}