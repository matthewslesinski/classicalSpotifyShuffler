using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ApplicationResources.Logging;
using ApplicationResources.Services;
using CustomResources.Utils.Concepts;
using CustomResources.Utils.Concepts.DataStructures;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;
using Microsoft.Extensions.DependencyInjection;
using GeneralUtils = CustomResources.Utils.GeneralUtils.Utils;

namespace ApplicationResources.ApplicationUtils
{
	public class CachedJSONData<T> : CachedData<T>
	{
		public CachedJSONData(string fileName, FileAccessType fileAccessType = FileAccessType.Basic, IDataStoreAccessor underlyingAccessor = null,
			bool useDefaultValue = false, T defaultValue = default)
			: base(fileName, ApplicationConstants<T>.JSONSerializer.Invert(), fileAccessType, underlyingAccessor: underlyingAccessor,
				  useDefaultValue: useDefaultValue, defaultValue: defaultValue)
		{ }
	}

	public class CachedData<T> : StandardDisposable, IDataAccessor
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
		private readonly Action<Exception> _persistExceptionHandler;

		private Reference<T> _cachedValue;
		private TaskCompletionSource<bool> _loadingTaskSource = null;

		public CachedData(string fileName, Bijection<string, T> parser, FileAccessType fileAccessType = FileAccessType.Flushing,
			IDataStoreAccessor underlyingAccessor = null, bool useDefaultValue = false, T defaultValue = default, Action<Exception> persistExceptionHandler = null) : this(fileName, parser, fileAccessType switch
			{
				FileAccessType.Basic => new BasicDataAccessor(fileName, underlyingAccessor),
				FileAccessType.Flushing => new FlushingDataAccessor(fileName, dataStoreAccessor: underlyingAccessor),
				FileAccessType.SlightlyLongFlushing => new FlushingDataAccessor(fileName, TimeSpan.FromSeconds(5), dataStoreAccessor: underlyingAccessor),
				_ => throw new NotImplementedException(),
			}, useDefaultValue, defaultValue, persistExceptionHandler)
		{ }

		public CachedData(string fileName, Bijection<string, T> parser, IDataAccessor localDataAccessor,
			bool useDefaultValue = false, T defaultValue = default, Action<Exception> persistExceptionHandler = null)
		{
			_parser = parser;
			_persistQueue = new(Persist);
			_localDataAccessor = localDataAccessor;
			Name = fileName;
			_useDefaultValue = useDefaultValue;
			_defaultValue = defaultValue;
			_persistExceptionHandler = persistExceptionHandler ?? (e => Logger.Error("An exception occurred while persisting cached value to data store: {exception}", e));
		}

		public string Name { get; }

		public bool IsLoaded => _loadingTaskSource != null && _loadingTaskSource.Task.IsCompleted;

		public bool Exists => _useDefaultValue || (IsLoaded ? _cachedValue != null : Exceptions.Throw<bool>(new NullReferenceException("The cached data must be initialized before accessing")));

		public T CachedValue
		{
			get
			{
				if (!IsLoaded)
				{
					if (_useDefaultValue)
						return _defaultValue;
					else
						throw new NullReferenceException("The cached data must be initialized before accessing");
				}
				else if (_cachedValue == null)
					throw new NullReferenceException("Cannot get a null value");
				return _cachedValue;
			}
			set
			{
				_ = SaveValueAsync(value);
			}
		}

		public Task<Result<string>> TryReadAsync(CancellationToken cancellationToken = default) => TryGetCachedValueAsync(cancellationToken).Transform(_parser.Inverse);
		public async Task<Result<T>> TryGetCachedValueAsync(CancellationToken cancellationToken = default)
		{
			if (!IsLoaded)
				await Initialize(cancellationToken).WithoutContextCapture();
			return Exists ? new(CachedValue) : Result<T>.NotFound;
		}

		public Task SaveAsync(string content, CancellationToken cancellationToken = default) => SaveValueAsync(_parser.Invoke(content), cancellationToken);
		public Task SaveValueAsync(T content, CancellationToken cancellationToken = default)
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
				if (currVal != null && Equals(currVal.Value, content))
					return Task.CompletedTask;
				// This is not necessarily thread/task safe. If two threads call set at the same time, one can set the _cachedValue first but
				// persist its value second
				if (Interlocked.CompareExchange(ref _cachedValue, content, currVal) == currVal)
				{
					if (!wasAlreadyLoaded)
						_loadingTaskSource.SetResult(false);
					var persistTask = _persistQueue.Schedule(content, cancellationToken);
					OnValueChanged?.Invoke(currVal, content);
					return persistTask;
				}
			}
		}

		public async Task<CachedData<T>> Initialize(CancellationToken cancellationToken = default)
		{
			bool wasLoaded = false;
			T loadedValue = default;
			var wasPerformed = await GeneralUtils.LoadOnceBlockingAsync(ref _loadingTaskSource, async () =>
			{
				var loadedResult = await Load(cancellationToken).WithoutContextCapture();
				Reference<T> valueToSet = loadedResult.DidFind ? loadedResult.FoundValue : (_useDefaultValue ? _defaultValue : null);
				wasLoaded = _cachedValue == null && Interlocked.CompareExchange(ref _cachedValue, valueToSet, null) == null;
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

		private async Task<Result<T>> Load(CancellationToken cancellationToken = default)
		{
			var result = await _localDataAccessor.TryReadAsync(cancellationToken).WithoutContextCapture();
			return result.Transform(_parser.Invoke);
		}

		private Task Persist(T value, CancellationToken cancellationToken = default)
		{
			try
			{
				var persistString = _parser.InvokeInverse(value);
				return _localDataAccessor.SaveAsync(persistString, cancellationToken);
			}
			catch (Exception e)
			{
				_persistExceptionHandler(e);
				throw;
			}
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

		public Task<Result<string>> TryReadAsync(CancellationToken cancellationToken = default) => _dataStoreAccessor.TryGetAsync(_dataKey, CachePolicy.AlwaysPreferCache, cancellationToken);

		public Task SaveAsync(string content, CancellationToken cancellationToken = default) => _dataStoreAccessor.SaveAsync(_dataKey, content, CachePolicy.AlwaysPreferCache, cancellationToken);

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

		public Task SaveAsync(string content, CancellationToken cancellationToken = default) { Add(content, cancellationToken); return Task.CompletedTask; }

		public Task<Result<string>> TryReadAsync(CancellationToken cancellationToken = default) => _underlyingAccessor.TryReadAsync(cancellationToken);

		protected override ReplacingContainer<string> CreateNewContainer() => new ReplacingContainer<string>();

		protected override async Task<AdditionalFlushOptions> Flush(ReplacingContainer<string> containerToFlush, CancellationToken cancellationToken = default)
		{
			await _underlyingAccessor.SaveAsync(containerToFlush.Contents, cancellationToken).WithoutContextCapture();
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
		Task<Result<string>> TryReadAsync(CancellationToken cancellationToken = default);
		Task SaveAsync(string content, CancellationToken cancellationToken = default);
	}
}
