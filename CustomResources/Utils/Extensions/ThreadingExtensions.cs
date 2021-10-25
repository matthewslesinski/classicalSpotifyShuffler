using System;
using System.Threading;
using CustomResources.Utils.GeneralUtils;

namespace CustomResources.Utils.Extensions
{
	public static class ThreadingExtensions
	{

        public static ReadLockToken ReadToken(this ReaderWriterLockSlim rwLock) => new ReadLockToken(rwLock);
        public static UpgradeableReadLockToken UpgradeableToken(this ReaderWriterLockSlim rwLock) => new UpgradeableReadLockToken(rwLock);
        public static WriteLockToken WriteToken(this ReaderWriterLockSlim rwLock) => new WriteLockToken(rwLock);

        public abstract class LockToken : IDisposable
		{
            protected ReaderWriterLockSlim _underlyingLock;
            public LockToken(ReaderWriterLockSlim underlyingLock)
            {
                Ensure.ArgumentNotNull(underlyingLock, nameof(underlyingLock));

                _underlyingLock = underlyingLock;
                EnterLock(_underlyingLock);
            }

            public void Dispose()
            {
                if (_underlyingLock != null)
                {
                    ExitLock(_underlyingLock);
                    _underlyingLock = null;
                    GC.SuppressFinalize(this);
                }
            }

            protected abstract void EnterLock(ReaderWriterLockSlim rwLock);
            protected abstract void ExitLock(ReaderWriterLockSlim rwLock);

            ~LockToken()
            {
                if (_underlyingLock != null)
                    Dispose();
            }
        }

        public sealed class ReadLockToken : LockToken
		{
            internal ReadLockToken(ReaderWriterLockSlim underlyingLock) : base(underlyingLock) { }

            protected override void EnterLock(ReaderWriterLockSlim rwLock) => rwLock.EnterReadLock();
            protected override void ExitLock(ReaderWriterLockSlim rwLock) => rwLock.ExitReadLock();
        }

        public sealed class UpgradeableReadLockToken : LockToken
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

        public sealed class WriteLockToken : LockToken
        {
            internal WriteLockToken(ReaderWriterLockSlim underlyingLock) : base(underlyingLock) { }

            protected override void EnterLock(ReaderWriterLockSlim rwLock) => rwLock.EnterWriteLock();
            protected override void ExitLock(ReaderWriterLockSlim rwLock) => rwLock.ExitWriteLock();
        }
    }
}
