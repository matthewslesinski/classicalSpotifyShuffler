using System;
using System.Threading;
using System.Threading.Tasks;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;

namespace CustomResources.Utils.Concepts.DataStructures
{
	public class AsyncLockProvider
	{
		private readonly AsyncLocal<bool> _alreadyEntered = new AsyncLocal<bool>();
		private TaskCompletionSource _taskSource = null;

		public AsyncLockProvider()
		{
			_alreadyEntered.Value = false;
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

		public bool TryEnterAloneImmediately()
		{
			TaskCompletionSource completionSource = null;
			return TryEnterAloneImmediately(ref completionSource);
		}

		public bool TryEnterAloneImmediately(ref TaskCompletionSource newSourceToUse)
		{
			if (_alreadyEntered.Value)
				throw new LockRecursionException("Attempting to enter a non-reentrant Async Lock");
			var entered = _taskSource == null
				&& Interlocked.CompareExchange(ref _taskSource, newSourceToUse ??= NewCompletionSource(), null) == null;
			if (entered)
				_alreadyEntered.Value = true;
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
			var exiting = currentSource != null && !currentSource.Task.IsCompleted;
			if (exiting)
				_alreadyEntered.Value = false;
			return exiting && currentSource.TrySetResult();
		}

		public Task<IDisposable> AcquireToken(CancellationToken cancellationToken = default) => new AsyncLockToken(this).Initialized(cancellationToken);

		public bool TryAcquireToken(out IDisposable acquiredToken)
		{
			if (TryEnterAloneImmediately())
			{
				acquiredToken = new AsyncLockToken(this);
				return true;
			}
			acquiredToken = null;
			return false;
		}

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
				AsyncLockProvider currentProvider;
				if ((currentProvider = Interlocked.Exchange(ref _underlyingProvider, null)) != null)
				{
					currentProvider.Exit();
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