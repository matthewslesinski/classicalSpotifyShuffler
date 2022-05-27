using System;
using System.Threading;
using System.Threading.Tasks;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;

namespace CustomResources.Utils.Concepts.DataStructures
{
	public class AsyncLockProvider
	{
		private readonly AsyncLocal<int> _numEntries = new AsyncLocal<int>();
		private TaskCompletionSource _taskSource = null;

		public AsyncLockProvider()
		{
			_numEntries.Value = 0;
		}

		public Task LockReleased
		{
			get
			{
				var currTaskSource = _taskSource;
				return currTaskSource == null ? Task.CompletedTask : currTaskSource.Task;
			}
		}

		public async Task EnterAlone(CancellationToken cancellationToken = default)
		{
			var didEnter = await TryEnterAlone(cancellationToken).WithoutContextCapture();
			if (!didEnter)
				cancellationToken.ThrowIfCancellationRequested();
		}

		public async Task<bool> TryEnterAlone(CancellationToken cancellationToken = default)
		{
			// Used so that a TaskCompletionSource doesn't need to be constructed every time we loop
			TaskCompletionSource newCompletionSource = null;
			if (cancellationToken.IsCancellationRequested)
				return false;
			while (!TryEnterAloneImmediately(ref newCompletionSource))
			{
				await LockReleased.WithoutContextCapture();
				if (cancellationToken.IsCancellationRequested)
					return false;
			}
			return true;
		}

		public bool TryEnterAloneImmediately(ref TaskCompletionSource newSourceToUse)
		{
			var entered = _taskSource == null
				&& Interlocked.CompareExchange(ref _taskSource, newSourceToUse ??= NewCompletionSource(), null) == null;
			return entered;
		}

		public void Exit()
		{
			if (!TryExit())
				throw new SynchronizationLockException("Cannot exit a lock when it is not locked");
		}

		private bool TryExit()
		{
			var currentSource = Interlocked.Exchange(ref _taskSource, null);
			return currentSource != null && currentSource.TrySetResult();
		}



		public Task<IDisposable> AcquireToken(CancellationToken cancellationToken = default) => new AsyncLockToken(this).Initialized(cancellationToken);


		private class AsyncLockToken : IDisposable
		{
			private AsyncLockProvider _underlyingProvider;

			public AsyncLockToken(AsyncLockProvider underlyingProvider)
			{
				Ensure.ArgumentNotNull(underlyingProvider, nameof(underlyingProvider));

				_underlyingProvider = underlyingProvider;
			}

			public async Task<IDisposable> Initialized(CancellationToken cancellationToken = default)
			{
				var enterLockTask = _underlyingProvider.EnterAlone(cancellationToken);
				await enterLockTask.WithoutContextCapture();
				return this;
			}


			public void Dispose()
			{
				if (Interlocked.Exchange(ref _underlyingProvider, null) != null)
				{
					_underlyingProvider.Exit();
					GC.SuppressFinalize(this);
				}
			}

			~AsyncLockToken()
			{
				if (_underlyingProvider != null)
					Dispose();
			}
		}


		private static TaskCompletionSource NewCompletionSource() => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
	}
}