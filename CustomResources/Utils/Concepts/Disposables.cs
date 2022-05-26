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

	public abstract class TaskContainingDisposable : StandardDisposable
	{
		private readonly CancellationTokenSource _tokenSource;
		private readonly CancellationTokenSource _combinedTokenSource;

		protected Task _workerTask;
		private int _isRunning = false.AsInt();

		public TaskContainingDisposable(CancellationToken externalCancellationToken = default)
		{
			_tokenSource = new CancellationTokenSource();
			_combinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_tokenSource.Token, externalCancellationToken);
		}

		public void Run(Action worker) => Run(() => { worker(); return Task.CompletedTask; });
		public void Run(Func<Task> worker)
		{
			if (_alreadyDisposed.AsBool())
				throw new InvalidOperationException("Already disposed");

			async Task Runner()
			{
				try
				{
					await worker();
				}
				finally
				{
					Interlocked.Exchange(ref _isRunning, false.AsInt());
				}
			}

			if (!Interlocked.CompareExchange(ref _isRunning, true.AsInt(), false.AsInt()).AsBool())
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
