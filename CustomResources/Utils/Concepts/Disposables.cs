using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;

namespace CustomResources.Utils.Concepts
{
	public abstract class StandardDisposable : IDisposable
	{
		protected int _alreadyDisposed = false.AsInt();
		public StandardDisposable() { }

		public void Dispose()
		{
			if (GeneralUtils.Utils.IsFirstRequest(ref _alreadyDisposed))
			{
				DoDispose();
				GC.SuppressFinalize(this);
			}
		}

		~StandardDisposable()
		{
			if (!_alreadyDisposed.AsBool())
				Dispose();
		}

		protected abstract void DoDispose();
	}

	public abstract class TaskContainingDisposable<OutputT> : StandardDisposable
	{
		private readonly CancellationTokenSource _tokenSource;
		private readonly CancellationToken _externalCancellationToken;

		protected Task<OutputT> _workerTask;
		protected bool IsRunning => _isRunning.AsBool();
		private int _isRunning = false.AsInt();

		public TaskContainingDisposable(CancellationToken externalCancellationToken = default)
		{
			_tokenSource = new CancellationTokenSource();
			_externalCancellationToken = externalCancellationToken;
		}

		public Task<Result<OutputT>> Run(Func<OutputT> worker, CancellationToken cancellationToken) => Run((_) => Task.FromResult(worker()), cancellationToken);
		public Task<Result<OutputT>> Run(Func<CancellationToken, Task<OutputT>> worker, CancellationToken cancellationToken = default)
		{
			if (_alreadyDisposed.AsBool())
				throw new InvalidOperationException("Already disposed");

			var taskCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _externalCancellationToken, _tokenSource.Token);

			async Task<OutputT> Runner()
			{
				try
				{
					return await worker(taskCancellationSource.Token).WithoutContextCapture();
				}
				finally
				{
					Interlocked.Exchange(ref _isRunning, false.AsInt());
				}
			}

			if (GeneralUtils.Utils.IsFirstRequest(ref _isRunning))
			{
				_workerTask = Task.Run(Runner, taskCancellationSource.Token);
				return _workerTask.Then(output => new Result<OutputT>(output));
			}
			return Task.FromResult(Result<OutputT>.Failure);
		}

		protected override void DoDispose()
		{
			_tokenSource.Cancel();
		}
	}

	public abstract class TaskContainingDisposable : TaskContainingDisposable<object>
	{
		public TaskContainingDisposable(CancellationToken externalCancellationToken = default) : base(externalCancellationToken)
		{ }

		public Task<bool> Run(Action worker, CancellationToken cancellationToken = default) => base.Run(() => { worker(); return null; }, cancellationToken).Then(result => result.Success);
		public Task<bool> Run(Func<CancellationToken, Task> worker, CancellationToken cancellationToken = default) =>
			base.Run(async (cancellationToken) => { await worker(cancellationToken).WithoutContextCapture(); return null; }, cancellationToken).Then(result => result.Success);
	}


	public class DisposableAction : StandardDisposable
	{
		private readonly Action _disposeAction;
		public DisposableAction(Action disposeAction)
		{
			Ensure.ArgumentNotNull(disposeAction, nameof(disposeAction));
			_disposeAction = disposeAction;
		}

		protected override void DoDispose()
		{
			_disposeAction();
		}
	}

	public sealed class MultipleDisposables : DisposableAction
	{
		public MultipleDisposables(IEnumerable<IDisposable> disposables) : base(() => disposables.EachIndependently(disposable => disposable.Dispose())) { }
	}
}
