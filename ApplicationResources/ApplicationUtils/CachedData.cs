using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ApplicationResources.Logging;
using ApplicationResources.Services;
using CustomResources.Utils.Concepts;
using CustomResources.Utils.Concepts.DataStructures;
using CustomResources.Utils.Extensions;
using Microsoft.Extensions.DependencyInjection;
using GeneralUtils = CustomResources.Utils.GeneralUtils.Utils;

namespace ApplicationResources.ApplicationUtils
{
	public class CachedJSONData<T> : CachedData<T>
	{
		public CachedJSONData(string fileName, FileAccessType fileAccessType = FileAccessType.Basic, bool useDefaultValue = false, T defaultValue = default)
			: base(fileName, ApplicationConstants<T>.JSONSerializer.Invert(), fileAccessType, useDefaultValue, defaultValue)
		{ }
	}

	public class CachedData<T> : StandardDisposable
	{
		public delegate void OnValueLoadedListener(T loadedValue);
		public delegate void OnValueChangedListener(T previousValue, T newValue);
		public event OnValueLoadedListener OnValueLoaded;
		public event OnValueChangedListener OnValueChanged;

		private readonly Bijection<string, T> _parser;
		private readonly IDataAccessor _localDataAccessor;
		private readonly CallbackTaskQueue<T> _persistQueue;
		private readonly bool _useDefaultValue;
		private readonly T _defaultValue;

		private Reference<T> _cachedValue;
		private TaskCompletionSource<bool> _loadingTaskSource = null;

		public CachedData(string fileName, Bijection<string, T> parser, FileAccessType fileAccessType = FileAccessType.Flushing,
			bool useDefaultValue = false, T defaultValue = default) : this(fileName, parser, fileAccessType switch
			{
				FileAccessType.Basic => new BasicDataAccessor(fileName),
				FileAccessType.Flushing => new FlushingDataAccessor(fileName),
				FileAccessType.SlightlyLongFlushing => new FlushingDataAccessor(fileName, TimeSpan.FromSeconds(5)),
				_ => throw new NotImplementedException(),
			}, useDefaultValue, defaultValue)
		{ }

		public CachedData(string fileName, Bijection<string, T> parser, IDataAccessor localDataAccessor,
			bool useDefaultValue = false, T defaultValue = default)
		{
			_parser = parser;
			_persistQueue = new((toPersist, _) => Persist(toPersist));
			_localDataAccessor = localDataAccessor;
			Name = fileName;
			_useDefaultValue = useDefaultValue;
			_defaultValue = defaultValue;
			OnValueChanged += (_, newValue) => _persistQueue.Schedule(newValue);
		}

		public string Name { get; }

		public bool IsLoaded => _loadingTaskSource != null && _loadingTaskSource.Task.IsCompleted;

		public T CachedValue
		{
			get
			{
				if (_cachedValue == null && !IsLoaded)
				{
					if (_useDefaultValue)
						return _defaultValue;
					else
						throw new NullReferenceException("The cached data must be initialized before accessing");
				}
				return _cachedValue;
			}
			set
			{
				bool wasAlreadyLoaded = true;
				if (_loadingTaskSource == null)
				{
					var initializedTaskSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
					wasAlreadyLoaded = Interlocked.CompareExchange(ref _loadingTaskSource, initializedTaskSource, null) != null;
				}
				while (true)
				{
					var currVal = _cachedValue;
					if (currVal != null && Equals(currVal.Value, value))
						break;
					// This is not necessarily thread/task safe. If two threads call set at the same time, one can set the _cachedValue first but
					// persist its value second
					else if (Interlocked.CompareExchange(ref _cachedValue, value, currVal) == currVal)
					{
						if (!wasAlreadyLoaded)
							_loadingTaskSource.SetResult(false);
						OnValueChanged?.Invoke(currVal, value);
						break;
					}
				}
			}
		}

		public async Task<CachedData<T>> Initialize()
		{
			bool wasLoaded = false;
			T loadedValue = default;
			var wasPerformed = await GeneralUtils.LoadOnceBlockingAsync(ref _loadingTaskSource, async () =>
			{
				wasLoaded = _cachedValue == null && Interlocked.CompareExchange(ref _cachedValue, await Load().WithoutContextCapture(), null) == null;
				if (wasLoaded)
					loadedValue = _cachedValue;
			}).WithoutContextCapture();
			if (wasPerformed && wasLoaded)
				OnValueLoaded?.Invoke(_cachedValue);
			return this;
		}

		protected override void DoDispose()
		{
			_persistQueue.Dispose();
			_localDataAccessor.Dispose();
		}

		private async Task<T> Load()
		{
			var (exists, foundContent) = await _localDataAccessor.TryReadAsync().WithoutContextCapture();
			return exists ? _parser.Invoke(foundContent) : default;
		}

		private Task Persist(T value)
		{
			var persistString = _parser.InvokeInverse(value);
			return _localDataAccessor.SaveAsync(persistString);
		}

		public enum FileAccessType
		{
			Basic,
			Flushing,
			SlightlyLongFlushing
		}
	}

	public class BasicDataAccessor : IDataAccessor
	{
		private readonly string _dataKey;
		private readonly IDataStoreAccessor _dataStoreAccessor;
		public BasicDataAccessor(string dataKey, IDataStoreAccessor dataStoreAccessor = null)
		{
			_dataKey = dataKey;
			_dataStoreAccessor = dataStoreAccessor ?? GlobalDependencies.Get<IDataStoreAccessor>();
		}

		public Task<Result<string>> TryReadAsync() => _dataStoreAccessor.TryGetAsync(_dataKey, CachePolicy.AlwaysPreferCache);

		public Task SaveAsync(string content) => _dataStoreAccessor.SaveAsync(_dataKey, content, CachePolicy.AlwaysPreferCache);

		public void Dispose()
		{
			// Do Nothing
		}
	}

	public class FlushingDataAccessor : Flusher<string, ReplacingContainer<string>>, IDataAccessor
	{
		private readonly IDataAccessor _underlyingAccessor;
		public FlushingDataAccessor(string dataKey, TimeSpan? flushWaitTime = null, IDataStoreAccessor dataStoreAccessor = null)
			: this(new BasicDataAccessor(dataKey, dataStoreAccessor), flushWaitTime)
		{ }
		public FlushingDataAccessor(IDataAccessor underlyingAccessor, TimeSpan? flushWaitTime = null)
			: base(flushWaitTime ?? TimeSpan.FromSeconds(1), true)
		{
			_underlyingAccessor = underlyingAccessor;
		}

		public Task SaveAsync(string content) { Add(content); return Task.CompletedTask; }

		public Task<Result<string>> TryReadAsync() => _underlyingAccessor.TryReadAsync();

		protected override ReplacingContainer<string> CreateNewContainer() => new ReplacingContainer<string>();

		protected override async Task<AdditionalFlushOptions> Flush(ReplacingContainer<string> containerToFlush)
		{
			await _underlyingAccessor.SaveAsync(containerToFlush.Contents).WithoutContextCapture();
			return AdditionalFlushOptions.NoAdditionalFlushNeeded;
		}

		protected override bool OnFlushFailed(Exception e)
		{
			Logger.Error("Failed to flush data: {exception}", e);
			return false;
		}
	}

	public interface IDataAccessor : IDisposable
	{
		Task<Result<string>> TryReadAsync();
		Task SaveAsync(string content);
	}
}
