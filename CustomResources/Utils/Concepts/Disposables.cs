using System;
using System.Collections.Generic;
using System.Threading;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;

namespace CustomResources.Utils.Concepts
{

	public class DisposableAction : IDisposable
	{
		private int _alreadyDisposed = 0;
		private readonly Action _disposeAction;
		public DisposableAction(Action disposeAction)
		{
			Ensure.ArgumentNotNull(disposeAction, nameof(disposeAction));
			_disposeAction = disposeAction;
		}

		public void Dispose()
		{
			if (Interlocked.Exchange(ref _alreadyDisposed, 1) == 0)
			{
				_disposeAction();
				GC.SuppressFinalize(this);
			}
		}

		~DisposableAction()
		{
			if (_alreadyDisposed == 0)
				Dispose();
		}
	}

	public sealed class MultipleDisposables : DisposableAction
	{
		public MultipleDisposables(IEnumerable<IDisposable> disposables) : base(() => disposables.EachIndependently(disposable => disposable.Dispose())) { }
	}
}
