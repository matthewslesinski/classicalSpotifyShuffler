using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CustomResources.Utils.GeneralUtils;

namespace CustomResources.Utils.Extensions
{
	public static class ThreadingExtensions
	{
        public static Func<Task> AndThen(this Func<Task> asyncAction, Action action) => TaskUtils.Compose(action, asyncAction);
        public static Func<Task<R>> AndThen<T, R>(this Func<Task<T>> asyncFunc, Func<T, R> func) => TaskUtils.Compose(func, asyncFunc);
        public static R Wait<R>(this Task<R> task) { task.Wait(); return task.Result; }

        public static Task InvokeAsync(this TaskUtils.AsyncEvent asyncEvent) =>
            Task.WhenAll(asyncEvent.GetAllCalls().Select(subscription => subscription()));
        public static Task InvokeAsync<ArgsT>(this TaskUtils.AsyncEvent<ArgsT> asyncEvent, ArgsT args) =>
            Task.WhenAll(asyncEvent.GetAllCalls().Select(subscription => subscription(args)));
        public static Task InvokeAsync<ArgsT>(this TaskUtils.AsyncEvent<object, ArgsT> asyncEvent, object sender, ArgsT args) =>
            Task.WhenAll(asyncEvent.GetAllCalls().Select(subscription => subscription(sender, args)));

        public static ReadLockToken ReadToken(this ReaderWriterLockSlim rwLock) => new ReadLockToken(rwLock);
        public static UpgradeableReadLockToken UpgradeableToken(this ReaderWriterLockSlim rwLock) => new UpgradeableReadLockToken(rwLock);
        public static WriteLockToken WriteToken(this ReaderWriterLockSlim rwLock) => new WriteLockToken(rwLock);

        public static MonitorLockToken FullLockToken(this object lockObj) => new MonitorLockToken(lockObj);

        public abstract class LockToken<LockT> : IDisposable where LockT : class
		{
            protected LockT _underlyingLock;
            public LockToken(LockT underlyingLock)
            {
                Ensure.ArgumentNotNull(underlyingLock, nameof(underlyingLock));

                _underlyingLock = underlyingLock;
                EnterLock(_underlyingLock);
            }

            public void Dispose()
            {
                LockT currentLock;
                if ((currentLock = Interlocked.Exchange(ref _underlyingLock, null)) != null)
                {
                    ExitLock(currentLock);
                    GC.SuppressFinalize(this);
                }
            }

            protected abstract void EnterLock(LockT @lock);
            protected abstract void ExitLock(LockT @lock);

            ~LockToken()
            {
                if (_underlyingLock != null)
                    Dispose();
            }
        }

        public sealed class ReadLockToken : LockToken<ReaderWriterLockSlim>
		{
            internal ReadLockToken(ReaderWriterLockSlim underlyingLock) : base(underlyingLock) { }

            protected override void EnterLock(ReaderWriterLockSlim rwLock) => rwLock.EnterReadLock();
            protected override void ExitLock(ReaderWriterLockSlim rwLock) => rwLock.ExitReadLock();
        }

        public sealed class UpgradeableReadLockToken : LockToken<ReaderWriterLockSlim>
        {
            internal UpgradeableReadLockToken(ReaderWriterLockSlim underlyingLock) : base(underlyingLock) { }

            protected override void EnterLock(ReaderWriterLockSlim rwLock) => rwLock.EnterUpgradeableReadLock();
            protected override void ExitLock(ReaderWriterLockSlim rwLock) => rwLock.ExitUpgradeableReadLock();

            public WriteLockToken Upgrade() => _underlyingLock.WriteToken();
            public ReadLockToken DownGrade()
			{
                var readToken = _underlyingLock.ReadToken();
                Dispose();
                return readToken;
			}
        }

        public sealed class WriteLockToken : LockToken<ReaderWriterLockSlim>
        {
            internal WriteLockToken(ReaderWriterLockSlim underlyingLock) : base(underlyingLock) { }

            protected override void EnterLock(ReaderWriterLockSlim rwLock) => rwLock.EnterWriteLock();
            protected override void ExitLock(ReaderWriterLockSlim rwLock) => rwLock.ExitWriteLock();
        }

        public sealed class MonitorLockToken : LockToken<object>
		{
            internal MonitorLockToken(object underlyingLock) : base(underlyingLock) { }

            protected override void EnterLock(object @lock) => Monitor.Enter(@lock);
            protected override void ExitLock(object @lock) => Monitor.Exit(@lock);
        }
    }
}