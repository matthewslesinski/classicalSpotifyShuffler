using System;
using System.Text;
using ApplicationResources.ApplicationUtils;
using ApplicationResources.Logging;
using ApplicationResources.Services;
using ApplicationResources.Setup;
using Blazored.LocalStorage;
using CustomResources.Utils.Concepts;
using CustomResources.Utils.Concepts.DataStructures;
using CustomResources.Utils.Extensions;

namespace BlazorApplicationResources.BlazorApplicationUtils
{
	public class LocalStorageLogger : Flusher<string, CombiningContainer<string, StringBuilder>>,  LoggerTargetProvider.ILogTarget
	{
		private readonly IDataStoreAccessor _browserStorage = new BrowserStorage();
		private readonly string _keyName;
		private string _latestString = "";
		private LogLevel _minLogLevel = Settings.Get<LogLevel>(BasicSettings.OutputFileLogLevel);

		public LocalStorageLogger(string keyName) : base(TimeSpan.FromMilliseconds(500), true)
		{
			_keyName = keyName;
		}

		public void Log(LoggerTargetProvider.LogActionArgs args)
		{
			if (args.Level >= _minLogLevel)
				Add(args.FullMessage);
		}

		protected override CombiningContainer<string, StringBuilder> CreateNewContainer()
		{
			return new((sb, str) => sb.Append(str).AppendLine(), new StringBuilder(_latestString));
		}

		protected override async Task<AdditionalFlushOptions> Flush(CombiningContainer<string, StringBuilder> containerToFlush, CancellationToken cancellationToken = default)
		{
			var stringToRecord = containerToFlush.Contents.ToString();
			_latestString = stringToRecord;
			await _browserStorage.SaveAsync(_keyName, stringToRecord, CachePolicy.AlwaysPreferCache, cancellationToken);
			return AdditionalFlushOptions.NoAdditionalFlushNeeded;
		}

		protected override bool OnFlushFailed(Exception e)
		{
			return false;
		}
	}

	internal class BrowserStorage : IDataStoreAccessor
	{
		public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken) => LocalStorage.ContainKeyAsync(key, cancellationToken).AsTask();

		public Task<string> GetAsync(string key, CachePolicy cachePolicy, CancellationToken cancellationToken) => LocalStorage.GetItemAsStringAsync(key, cancellationToken).AsTask();

		public Task<bool> SaveAsync(string key, string data, CachePolicy cachePolicy, CancellationToken cancellationToken) =>
			(data == null ? LocalStorage.RemoveItemAsync(key, cancellationToken).AsTask() : LocalStorage.SetItemAsStringAsync(key, data, cancellationToken).AsTask()).Then(() => true);

		private static ILocalStorageService LocalStorage => GlobalDependencies.Get<ILocalStorageService>();
	}
}