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
		public Task<Result<string>> TryGetAsync(string key) => TryGetAsync(key, default);
		public async Task<Result<string>> TryGetAsync(string key, CancellationToken cancellationToken) =>
			await ExistsAsync(key, cancellationToken).WithoutContextCapture()
				? new(true, await GetAsync(key, cancellationToken).WithoutContextCapture())
				: Result<string>.NotFound;

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
		public Task<bool> ExistsAsync(string key, CancellationToken _) => Task.FromResult(Exists(key));
		public string Get(string key) => File.ReadAllText(key);
		public Task<string> GetAsync(string key, CancellationToken cancellationToken) => File.ReadAllTextAsync(key, cancellationToken);
		public bool Save(string key, string data)
		{
			if (data == null)
				File.Delete(key);
			else
				File.WriteAllText(key, data);
			return true;
		}
		public async Task<bool> SaveAsync(string key, string data, CancellationToken cancellationToken)
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
		public static Task SaveAllLinesAsync(this IDataStoreAccessor dataStore, string key, IEnumerable<string> lines) =>
			dataStore.SaveAsync(key, lines == null ? null : string.Join('\n', lines));
	}
}

