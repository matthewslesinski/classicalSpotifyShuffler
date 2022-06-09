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
		private readonly CancellationTokenSource _combinedTokenSource;

		protected Task<OutputT> _workerTask;
		protected bool IsRunning => _isRunning.AsBool();
		private int _isRunning = false.AsInt();

		public TaskContainingDisposable(CancellationToken externalCancellationToken = default)
		{
			_tokenSource = new CancellationTokenSource();
			_combinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_tokenSource.Token, externalCancellationToken);
		}

		public Task<Result<OutputT>>Run(Func<OutputT> worker) => Run(() => Task.FromResult(worker()));
		public Task<Result<OutputT>> Run(Func<Task<OutputT>> worker)
		{
			if (_alreadyDisposed.AsBool())
				throw new InvalidOperationException("Already disposed");

			async Task<OutputT> Runner()
			{
				try
				{
					return await worker().WithoutContextCapture();
				}
				finally
				{
					Interlocked.Exchange(ref _isRunning, false.AsInt());
				}
			}

			if (GeneralUtils.Utils.IsFirstRequest(ref _isRunning))
			{
				_workerTask = Task.Run(Runner, StopToken);
				return _workerTask.Then(output => new Result<OutputT>(output));
			}
			return Task.FromResult(Result<OutputT>.Failure);
		}

		protected CancellationToken StopToken => _combinedTokenSource.Token;

		protected override void DoDispose()
		{
			_tokenSource.Cancel();
		}
	}

	public abstract class TaskContainingDisposable : TaskContainingDisposable<object>
	{
		public TaskContainingDisposable(CancellationToken externalCancellationToken = default) : base(externalCancellationToken)
		{ }

		public Task<bool> Run(Action worker) => base.Run(() => { worker(); return null; }).Then(result => result.Success);
		public Task<bool> Run(Func<Task> worker) => Run(async () => { await worker().WithoutContextCapture(); return null; }).Then(result => result.Success);
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
