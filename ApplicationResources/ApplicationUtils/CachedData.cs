using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ApplicationResources.Services;
using CustomResources.Utils.Concepts;
using CustomResources.Utils.Concepts.DataStructures;
using CustomResources.Utils.Extensions;
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
		private readonly IDataAccessor _fileAccessor;
		private readonly CallbackTaskQueue<T> _persistQueue;
		private readonly bool _useDefaultValue;
		private readonly T _defaultValue;

		private Reference<T> _cachedValue;
		private TaskCompletionSource<bool> _loadingTaskSource = null;

		public CachedData(string fileName, Bijection<string, T> parser, FileAccessType fileAccessType = FileAccessType.Flushing,
			bool useDefaultValue = false, T defaultValue = default)
		{
			_parser = parser;
			_persistQueue = new((toPersist, _) => Persist(toPersist));
			_fileAccessor = fileAccessType switch
			{
				FileAccessType.Basic => new BasicDataAccessor(fileName),
				FileAccessType.Flushing => new FlushingDataAccessor(fileName),
				FileAccessType.SlightlyLongFlushing => new FlushingDataAccessor(fileName, TimeSpan.FromSeconds(5)),
				_ => throw new NotImplementedException(),
			};
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
			_fileAccessor.Dispose();
		}

		private async Task<T> Load()
		{
			var (exists, foundContent) = await _fileAccessor.TryReadAsync().WithoutContextCapture();
			return exists ? _parser.Invoke(foundContent) : default;
		}

		private Task Persist(T value)
		{
			var persistString = _parser.InvokeInverse(value);
			return _fileAccessor.SaveAsync(persistString);
		}

		private class BasicDataAccessor : IDataAccessor
		{
			private readonly string _dataKey;
			private readonly IDataStoreAccessor _dataStoreAccessor;
			internal BasicDataAccessor(string dataKey, IDataStoreAccessor dataStoreAccessor = null)
			{
				_dataKey = dataKey;
				_dataStoreAccessor = dataStoreAccessor ?? new FileAccessor();
			}

			public Task<(bool exists, string foundContent)> TryReadAsync() => _dataStoreAccessor.TryGetAsync(_dataKey);

			public Task SaveAsync(string content) => _dataStoreAccessor.SaveAsync(_dataKey, content);

			public void Dispose()
			{
				// Do Nothing
			}
		}

		private class FlushingDataAccessor : Flusher<string, DataWrapper>, IDataAccessor
		{
			private readonly IDataAccessor _underlyingAccessor;
			internal FlushingDataAccessor(string dataKey, TimeSpan? flushWaitTime = null, IDataStoreAccessor dataStoreAccessor = null)
				: this(new BasicDataAccessor(dataKey, dataStoreAccessor), flushWaitTime)
			{ }
			internal FlushingDataAccessor(IDataAccessor underlyingAccessor, TimeSpan? flushWaitTime = null) : base(flushWaitTime ?? TimeSpan.FromSeconds(1), true)
			{
				_underlyingAccessor = underlyingAccessor;
			}

			public Task SaveAsync(string content) { Add(content); return Task.CompletedTask; }

			public Task<(bool exists, string foundContent)> TryReadAsync() => _underlyingAccessor.TryReadAsync();

			protected override DataWrapper CreateNewContainer() => new DataWrapper();

			protected override Task<AdditionalFlushOptions> Flush(DataWrapper containerToFlush)
			{
				_underlyingAccessor.SaveAsync(containerToFlush.Contents);
				return Task.FromResult(AdditionalFlushOptions.NoAdditionalFlushNeeded);
			}
		}

		private class DataWrapper : IFlushableContainer<string>
		{
			private string _contents;
			private Reference<bool> _isFlushScheduled = false;

			internal string Contents => _contents;

			public bool RequestFlush() => GeneralUtils.IsFirstRequest(ref _isFlushScheduled);

			public bool Update(string itemToFlush) { Interlocked.Exchange(ref _contents, itemToFlush); return true; }
		}

		public enum FileAccessType
		{
			Basic,
			Flushing,
			SlightlyLongFlushing
		}
	}

	public interface IDataAccessor : IDisposable
	{
		Task<(bool exists, string foundContent)> TryReadAsync();
		Task SaveAsync(string content);
	}
}
