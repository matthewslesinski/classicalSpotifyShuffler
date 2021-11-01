using System;
using System.Collections.Generic;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;

namespace CustomResources.Utils.Concepts
{

	public class DisposableAction : IDisposable
	{
		private bool _alreadyDisposed = false;
		private readonly Action _disposeAction;
		public DisposableAction(Action disposeAction)
		{
			Ensure.ArgumentNotNull(disposeAction, nameof(disposeAction));
			_disposeAction = disposeAction;
		}

		public void Dispose()
		{
			if (!_alreadyDisposed)
			{
				_disposeAction();
				_alreadyDisposed = true;
				GC.SuppressFinalize(this);

			}
		}

		~DisposableAction()
		{
			if (!_alreadyDisposed)
				Dispose();
		}
	}

	public sealed class MultipleDisposables : DisposableAction
	{
		public MultipleDisposables(IEnumerable<IDisposable> disposables) : base(() => disposables.EachIndependently(disposable => disposable.Dispose())) { }
	}
}
