using System;
using System.Threading;
using System.Threading.Tasks;
using CustomResources.Utils.GeneralUtils;

namespace CustomResources.Utils.Extensions
{
	public static class ThreadingExtensions
	{

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
                if (Interlocked.Exchange(ref _underlyingLock, null) != null)
                {
                    ExitLock(_underlyingLock);
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
