using System;
using System.Collections.Generic;
using System.Threading;
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
		public TaskContainingDisposable()
		{
			_tokenSource = new CancellationTokenSource();
		}

		protected CancellationToken DisposeToken => _tokenSource.Token;

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
