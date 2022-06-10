using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ApplicationResources.Logging;
using ApplicationResources.Services;
using CustomResources.Utils.Concepts;
using CustomResources.Utils.Concepts.DataStructures;
using CustomResources.Utils.Extensions;

namespace ApplicationResources.ApplicationUtils
{
	public class CachedLocalStorage : IDataStoreAccessor
	{
		private readonly InternalConcurrentDictionary<string, CachedData<string>> _caches = new();
		private readonly Func<string, CachedData<string>> _initializer;

		public CachedLocalStorage(Func<string, CachedData<string>> initializer)
		{
			_initializer = initializer;
		}

		public CachedLocalStorage(CachedData<string>.FileAccessType fileAccessType, IDataStoreAccessor underlyingAccessor = null)
			: this(fileName => new CachedData<string>(fileName, Bijections<string>.Identity, fileAccessType, underlyingAccessor, persistExceptionHandler: e => OnPersistException(e, fileName)))
		{ }

		public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken) =>
			PerformOperationOnCache(key, (cache, _) => Task.FromResult(cache.Exists), cancellationToken);

		public Task<string> GetAsync(string key, CachePolicy _, CancellationToken cancellationToken) =>
			PerformOperationOnCache(key, (cache, _) => Task.FromResult(cache.CachedValue), cancellationToken);

		public Task<bool> SaveAsync(string key, string data, CachePolicy cachePolicy, CancellationToken cancellationToken) =>
			PerformOperationOnCache(key, (cache, cancellationToken) => cache.SaveValueAsync(data, cancellationToken).Then(() => true), cancellationToken);

		private async Task<CachedData<string>> GetOrAddCache(string key, CancellationToken cancellationToken = default)
		{
			if (!_caches.TryGetValue(key, out var cache))
			{
				cache = _caches.GetOrAdd(key, _initializer);
				await cache.Initialize(cancellationToken).WithoutContextCapture();
			}
			return cache;
		}

		private async Task<R> PerformOperationOnCache<R>(string key, Func<CachedData<string>, CancellationToken, Task<R>> operation, CancellationToken cancellationToken = default)
		{
			var cache = await GetOrAddCache(key, cancellationToken).WithoutContextCapture();
			return await operation(cache, cancellationToken).WithoutContextCapture();
		}

		private static void OnPersistException(Exception e, string fileName)
		{
			Logger.Error("An Exception occurred file saving content to file {fileName}", fileName);
		}
	}
}