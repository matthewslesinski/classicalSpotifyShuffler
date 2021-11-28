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
		protected int _alreadyDisposed = 0;
		public StandardDisposable() { }

		public void Dispose()
		{
			if (Interlocked.Exchange(ref _alreadyDisposed, 1) == 0)
			{
				DoDispose();
				GC.SuppressFinalize(this);
			}
		}

		~StandardDisposable()
		{
			if (_alreadyDisposed == 0)
				Dispose();
		}

		protected abstract void DoDispose();
	}

	public abstract class TaskContainingDisposable : StandardDisposable
	{
		private readonly CancellationTokenSource _tokenSource;
		private readonly CancellationTokenSource _combinedTokenSource;

		protected Task _workerTask;
		private int _isRunning = 0;

		public TaskContainingDisposable(CancellationToken externalCancellationToken = default)
		{
			_tokenSource = new CancellationTokenSource();
			_combinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_tokenSource.Token, externalCancellationToken);
		}

		public void Run(Action worker) => Run(() => { worker(); return Task.CompletedTask; });
		public void Run(Func<Task> worker)
		{
			if (_alreadyDisposed == 1)
				throw new InvalidOperationException("Already disposed");

			async Task Runner()
			{
				try
				{
					await worker();
				}
				finally
				{
					Interlocked.Exchange(ref _isRunning, 0);
				}
			}

			if (Interlocked.CompareExchange(ref _isRunning, 1, 0) == 0)
				_workerTask = Task.Run(Runner, StopToken);
		}

		protected CancellationToken StopToken => _combinedTokenSource.Token;

		protected override void DoDispose()
		{
			_tokenSource.Cancel();
		}
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
