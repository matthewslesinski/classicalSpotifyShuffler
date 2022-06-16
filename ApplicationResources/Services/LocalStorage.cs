using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CustomResources.Utils.Concepts;
using CustomResources.Utils.Extensions;

namespace ApplicationResources.Services
{
	public interface IDataStoreAccessor
	{
		public Task<Result<string>> TryGetAsync(string key, CachePolicy cachePolicy) => TryGetAsync(key, cachePolicy, default);
		public async Task<Result<string>> TryGetAsync(string key, CachePolicy cachePolicy, CancellationToken cancellationToken) =>
			await ExistsAsync(key, cancellationToken).WithoutContextCapture()
				? new(true, await GetAsync(key, cachePolicy, cancellationToken).WithoutContextCapture())
				: Result<string>.NotFound;

		public Task<bool> ExistsAsync(string key) => ExistsAsync(key, default);
		Task<bool> ExistsAsync(string key, CancellationToken cancellationToken);
		public Task<string> GetAsync(string key, CachePolicy cachePolicy) => GetAsync(key, cachePolicy, default);
		Task<string> GetAsync(string key, CachePolicy cachePolicy, CancellationToken cancellationToken);
		public Task<bool> SaveAsync(string key, string data, CachePolicy cachePolicy) => SaveAsync(key, data, cachePolicy, default);
		Task<bool> SaveAsync(string key, string data, CachePolicy cachePolicy, CancellationToken cancellationToken);
	}

	public enum CachePolicy {
		DontCache,
		PreferActual,
		AlwaysPreferCache
	}

	public interface ISynchronousDataAccessor : IDataStoreAccessor
	{
		public bool TryGet(string key, out string data, CachePolicy cachePolicy)
		{
			if (Exists(key))
			{
				data = Get(key, cachePolicy);
				return true;
			}
			data = default;
			return false;
		}

		bool Exists(string key);
		string Get(string key, CachePolicy cachePolicy);

		bool Save(string key, string data, CachePolicy cachePolicy);
	}

	public class FileAccessor : ISynchronousDataAccessor
	{
		public bool Exists(string key) => File.Exists(key);
		public Task<bool> ExistsAsync(string key, CancellationToken _) => Task.FromResult(Exists(key));
		public string Get(string key, CachePolicy _) => File.ReadAllText(key);
		public Task<string> GetAsync(string key, CachePolicy _, CancellationToken cancellationToken) => File.ReadAllTextAsync(key, cancellationToken);
		public bool Save(string key, string data, CachePolicy _)
		{
			if (data == null)
				File.Delete(key);
			else
				File.WriteAllText(key, data);
			return true;
		}
		public async Task<bool> SaveAsync(string key, string data, CachePolicy _, CancellationToken cancellationToken)
		{
			if (data == null)
				File.Delete(key);
			else
				await File.WriteAllTextAsync(key, data, cancellationToken).WithoutContextCapture();
			return true;
		}
	}

	public static class DataStoreExtensions
	{
		public static Task SaveAllLinesAsync(this IDataStoreAccessor dataStore, string key, IEnumerable<string> lines, CachePolicy cachePolicy, CancellationToken cancellationToken = default) =>
			dataStore.SaveAsync(key, lines == null ? null : string.Join('\n', lines), cachePolicy, cancellationToken);
	}
}

