using System;
using ApplicationResources.Logging;
using ApplicationResources.Services;
using Blazored.LocalStorage;
using CustomResources.Utils.Concepts;
using CustomResources.Utils.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace ClassicalSpotifyShuffler.Utils
{
	public class LocalStorageAccessor : IDataStoreAccessor
	{
		private static ILocalStorageService BrowserLocalStorage => GlobalDependencies.Get<ILocalStorageService>();
		private static HttpClient Server => GlobalDependencies.Get<HttpClient>();
		public LocalStorageAccessor()
		{
		}

		public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken)
		{
			var localStorageTask = BrowserContains(key, cancellationToken);
			var serverTask = ServerContains(key, cancellationToken);
			return await localStorageTask.WithoutContextCapture() || await serverTask.WithoutContextCapture();
		}

		public Task<string?> GetAsync(string key, CachePolicy cachePolicy, CancellationToken cancellationToken)
		{
			return TryGetAsync(key, cachePolicy, cancellationToken).Then(result => result.DidFind ? result.FoundValue : null);
		}

		public async Task<bool> SaveAsync(string key, string data, CachePolicy cachePolicy, CancellationToken cancellationToken)
		{
			if (cachePolicy == CachePolicy.DontCache)
				return false;

			if (data == null)
				await BrowserLocalStorage.RemoveItemAsync(key, cancellationToken).AsTask().WithoutContextCapture();
			else
				await BrowserLocalStorage.SetItemAsStringAsync(key, data, cancellationToken).AsTask().WithoutContextCapture();
			return true;
		}

		private static Task<bool> BrowserContains(string key, CancellationToken cancellationToken) =>
			BrowserLocalStorage.ContainKeyAsync(key, cancellationToken).AsTask();

		private static Task<bool> ServerContains(string key, CancellationToken cancellationToken) =>
			Server.GetAsync(key, HttpCompletionOption.ResponseHeadersRead, cancellationToken).Then(result => result.IsSuccessStatusCode);

		private static async Task<Result<string>> TryGetFromServer(string key, CancellationToken cancellationToken)
		{
			if (await ServerContains(key, cancellationToken).WithoutContextCapture())
				return new(await Server.GetStringAsync(key, cancellationToken).WithoutContextCapture());
			return Result<string>.NotFound;
		}

		private static async Task<Result<string>> TryGetFromBrowser(string key, CancellationToken cancellationToken)
		{
			if (await BrowserContains(key, cancellationToken).WithoutContextCapture())
				return new(await BrowserLocalStorage.GetItemAsStringAsync(key, cancellationToken).WithoutContextCapture());
			return Result<string>.NotFound;
		}

		Task<Result<string>> IDataStoreAccessor.TryGetAsync(string key, CachePolicy cachePolicy, CancellationToken cancellationToken) =>
			TryGetAsync(key, cachePolicy, cancellationToken);

		public async Task<Result<string>> TryGetAsync(string key, CachePolicy cachePolicy, CancellationToken cancellationToken = default)
		{
			if (cachePolicy != CachePolicy.AlwaysPreferCache)
			{
				var serverTask = TryGetFromServer(key, cancellationToken).WithoutContextCapture();
				var browserTask = TryGetFromBrowser(key, cancellationToken).WithoutContextCapture();
				var serverResult = await serverTask;
				if (serverResult.DidFind)
					return serverResult;
				var browserResult = await browserTask;
				if (browserResult.DidFind)
					return browserResult;
			}
			else
			{
				var browserTask = TryGetFromBrowser(key, cancellationToken).WithoutContextCapture();
				var browserResult = await browserTask;
				if (browserResult.DidFind)
					return browserResult;
				var serverTask = TryGetFromServer(key, cancellationToken).WithoutContextCapture();
				var serverResult = await serverTask;
				if (serverResult.DidFind)
				{
					await SaveAsync(key, serverResult.FoundValue, cachePolicy, cancellationToken).WithoutContextCapture();
					return serverResult;
				}
			}
			return Result<string>.NotFound;
		}
	}
}