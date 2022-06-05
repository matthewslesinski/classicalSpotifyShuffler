using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CustomResources.Utils.Extensions;

namespace ApplicationResources.Services
{
	public interface IDataStoreAccessor
	{
		public Task<(bool foundData, string data)> TryGetAsync(string key) => TryGetAsync(key, default);
		public async Task<(bool foundData, string data)> TryGetAsync(string key, CancellationToken cancellationToken) =>
			await ExistsAsync(key, cancellationToken).WithoutContextCapture()
				? (true, await GetAsync(key, cancellationToken).WithoutContextCapture())
				: (false, null);

		public Task<bool> ExistsAsync(string key) => ExistsAsync(key, default);
		Task<bool> ExistsAsync(string key, CancellationToken cancellationToken);
		public Task<string> GetAsync(string key) => GetAsync(key, default);
		Task<string> GetAsync(string key, CancellationToken cancellationToken);
		public Task<bool> SaveAsync(string key, string data) => SaveAsync(key, data, default);
		Task<bool> SaveAsync(string key, string data, CancellationToken cancellationToken);

	}

	public interface ISynchronousDataAccessor : IDataStoreAccessor
	{
		public bool TryGet(string key, out string data)
		{
			if (Exists(key))
			{
				data = Get(key);
				return true;
			}
			data = default;
			return false;
		}

		bool Exists(string key);
		string Get(string key);

		bool Save(string key, string data);
	}

	public class FileAccessor : ISynchronousDataAccessor
	{
		public bool Exists(string key) => File.Exists(key);
		public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken) => Task.FromResult(Exists(key));
		public string Get(string key) => File.ReadAllText(key);
		public Task<string> GetAsync(string key, CancellationToken cancellationToken) => File.ReadAllTextAsync(key, cancellationToken);
		public bool Save(string key, string data) { File.WriteAllText(key, data); return true; }
		public async Task<bool> SaveAsync(string key, string data, CancellationToken cancellationToken) { await File.WriteAllTextAsync(key, data, cancellationToken); return true; }
	}

	public static class DataStoreExtensions
	{
		public static Task SaveAllLinesAsync(this IDataStoreAccessor dataStore, string key, IEnumerable<string> lines) =>
			dataStore.SaveAsync(key, string.Join('\n', lines));
	}
}

