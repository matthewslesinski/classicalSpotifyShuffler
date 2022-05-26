﻿using System;
using ApplicationResources.Services;
using Blazored.LocalStorage;
using CustomResources.Utils.Extensions;

namespace ClassicalSpotifyShuffler.Utils
{
	public class LocalStorageAccessor : IDataStoreAccessor
	{
		private static ILocalStorageService BrowserLocalStorage => GlobalDependencies.GlobalDependencyContainer.GetRequiredService<ILocalStorageService>();
		private static HttpClient Server => GlobalDependencies.GlobalDependencyContainer.GetRequiredService<HttpClient>();
		public LocalStorageAccessor()
		{
		}

		public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken)
		{
			var localStorageTask = BrowserLocalStorage.ContainKeyAsync(key, cancellationToken).WithoutContextCapture();
			var serverTask = Server.GetStringAsync(key, cancellationToken).WithoutContextCapture();
			return await localStorageTask || await serverTask != null;
		}

		public async Task<string?> GetAsync(string key, CancellationToken cancellationToken)
		{
			// TODO Make this work better, and cache files in LocalStorage
			var serverValue = await Server.GetStringAsync(key, cancellationToken).WithoutContextCapture();
			if (serverValue != null)
				return serverValue;
			if (await BrowserLocalStorage.ContainKeyAsync(key, cancellationToken).WithoutContextCapture())
				return await BrowserLocalStorage.GetItemAsStringAsync(key, cancellationToken).WithoutContextCapture();
			return default;
		}

		public async Task<bool> SaveAsync(string key, string data, CancellationToken cancellationToken)
		{
			await BrowserLocalStorage.SetItemAsStringAsync(key, data, cancellationToken).AsTask().WithoutContextCapture();
			return true;
		}

		async Task<(bool foundData, string? data)> IDataStoreAccessor.TryGetAsync(string key, CancellationToken cancellationToken)
		{
			var getTask = GetAsync(key, cancellationToken).WithoutContextCapture();
			var data = await getTask;
			return (data != null, data);
		}
	}
}

