using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;

namespace CustomResources.Utils.Concepts.DataStructures
{
	public abstract class TaskQueue<InputT, OutputT> : TaskContainingDisposable
	{
		private readonly Channel<Node> _queue = Channel.CreateUnbounded<Node>(new UnboundedChannelOptions() { AllowSynchronousContinuations = true, SingleReader = true });

		private int _isRunning = true.AsInt();

		public TaskQueue(CancellationToken cancellationToken = default) : base(cancellationToken)
		{
			Run(Process);
		}

		public void StopRunning()
		{
			if (Interlocked.Exchange(ref _isRunning, false.AsInt()).AsBool())
			{
				_queue.Writer.Complete();
				while (_queue.Reader.TryRead(out var node))
					node.TaskCompleter.SetCanceled();
			}
		}

		protected override void DoDispose() => StopRunning();

		public async Task<OutputT> Schedule(InputT input, CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();
			CheckWorkerState();
			var taskCompleter = new TaskCompletionSource<OutputT>(TaskCreationOptions.RunContinuationsAsynchronously);
			var ec = ExecutionContext.Capture();
			var node = new Node(input, taskCompleter, ec, cancellationToken);
			await _queue.Writer.WriteAsync(node, cancellationToken).WithoutContextCapture();
			return await taskCompleter.Task.WithoutContextCapture();
		}

		private async Task Process(CancellationToken generalCancellationToken = default)
		{
			generalCancellationToken.Register(StopRunning);
			await foreach (var (input, taskCompleter, executionContext, taskCancellationToken) in _queue.Reader.ReadAllAsync(generalCancellationToken).WithoutContextCapture())
			{
				if (taskCancellationToken.IsCancellationRequested)
					taskCompleter.SetCanceled(taskCancellationToken);
				else if (generalCancellationToken.IsCancellationRequested)
					taskCompleter.SetCanceled(generalCancellationToken);
				else if (_alreadyDisposed.AsBool() || !_isRunning.AsBool())
					taskCompleter.SetCanceled(generalCancellationToken);
				else
				{
					try
					{
						await TaskUtils.RunAndNotify(taskCompleter, cancellationToken =>
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

							return handler;
						}, taskCancellationToken).WithoutContextCapture();
					}
					catch (Exception)
					{
						// Ignore, as it should be passed onto the inner task already
					}
				}
			}
		}

		protected abstract Task<OutputT> HandleTask(InputT input, CancellationToken taskCancellationToken);

		private record struct Node(InputT Input, TaskCompletionSource<OutputT> TaskCompleter, ExecutionContext ExecutionContext, CancellationToken TaskCancellationToken);

		private void CheckWorkerState()
		{
			if (!_workerTask.IsCompleted)
				return;
			if (_workerTask.IsFaulted)
				throw new Exception($"{GetType().Name}: Cannot proceed because the worker task terminated due to an exception", _workerTask.Exception);
			if (_alreadyDisposed.AsBool())
				throw new OperationCanceledException($"{GetType().Name}: Cannot proceed because the queue has been completed");
			else
				throw new Exception($"{GetType().Name}: Cannot proceed because the queue has been incorrectly finished");		
		}
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