using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CustomResources.Utils.Concepts;
using CustomResources.Utils.Concepts.DataStructures;
using GeneralUtils = CustomResources.Utils.GeneralUtils.Utils;

namespace ApplicationResources.ApplicationUtils
{
	public class CachedJSONData<T> : CachedData<T>
	{
		public CachedJSONData(string fileName, FileAccessType fileAccessType = FileAccessType.Basic)
			: base(fileName, ApplicationConstants<T>.JSONSerializer.Invert(), fileAccessType)
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

		private Reference<T> _cachedValue;
		private bool _isLoaded = false;
		private readonly object _loadLock = new object();

		public CachedData(string fileName, Bijection<string, T> parser, FileAccessType fileAccessType = FileAccessType.Flushing)
		{
			_parser = parser;
			_fileAccessor = fileAccessType switch
			{
				FileAccessType.Basic => new BasicDataAccessor(fileName),
				FileAccessType.Flushing => new FlushingDataAccessor(fileName),
				FileAccessType.SlightlyLongFlushing => new FlushingDataAccessor(fileName, TimeSpan.FromSeconds(5)),
				_ => throw new NotImplementedException(),
			};
			Name = fileName;
			OnValueChanged += (_, newValue) => Persist(newValue);
			
		}

		public string Name { get; }

		public T CachedValue
		{
			get
			{
				if (_cachedValue == null && !_isLoaded)
				{
					GeneralUtils.LoadOnce(ref _isLoaded, _loadLock, () =>
					{
						if (InitializeField(ref _cachedValue, () => Load(), out var loadedValue))
							OnValueLoaded?.Invoke(loadedValue);
					});
				}
				return _cachedValue;
			}
			set
			{
				while (true) {
					var currVal = _cachedValue;
					if (currVal != null && Equals(currVal.Value, value))
						break;
					else if (Interlocked.CompareExchange(ref _cachedValue, value, currVal) == currVal)
					{
						OnValueChanged?.Invoke(currVal, value);
						break;
					}
				}
			}
		}

		protected override void DoDispose()
		{
			_fileAccessor.Dispose();
		}

		private T Load()
		{
			return _fileAccessor.TryRead(out var foundContent) ? _parser.Invoke(foundContent) : default;
		}

		private void Persist(T value)
		{
			var persistString = _parser.InvokeInverse(value);
			_fileAccessor.Save(persistString);
		}

		private static bool InitializeField<F>(ref F field, Func<F> loadFunc, out F loadedField) where F : class
		{
			F loadedValue;
			if (field == null && Interlocked.CompareExchange(ref field, loadedValue = loadFunc(), null) == null)
			{
				loadedField = loadedValue;
				return true;
			}
			loadedField = default;
			return false;
		}

		private class BasicDataAccessor : IDataAccessor
		{
			private readonly string _dataKey;
			private readonly bool _shouldWriteAsync;
			internal BasicDataAccessor(string fileName, bool shouldWriteAsync = true)
			{
				_dataKey = fileName;
				_shouldWriteAsync = shouldWriteAsync;
			}

			public bool TryRead(out string foundContent)
			{
				if (!File.Exists(_dataKey))
				{
					foundContent = null;
					return false;
				}
				foundContent = File.ReadAllText(_dataKey);
				return true;
			}

			public void Save(string content)
			{
				if (_shouldWriteAsync)
					File.WriteAllTextAsync(_dataKey, content);
				else
					File.WriteAllText(_dataKey, content);
			}

			public void Dispose()
			{
				// Do Nothing
			}
		}

		private class FlushingDataAccessor : Flusher<string, DataWrapper>, IDataAccessor
		{
			private readonly IDataAccessor _underlyingAccessor;
			internal FlushingDataAccessor(string dataKey, TimeSpan? flushWaitTime = null) : this(new BasicDataAccessor(dataKey, false), flushWaitTime) { }
			internal FlushingDataAccessor(IDataAccessor underlyingAccessor, TimeSpan? flushWaitTime = null) : base(flushWaitTime ?? TimeSpan.FromSeconds(1), true)
			{
				_underlyingAccessor = underlyingAccessor;
			}

			public void Save(string content) => Add(content);

			public bool TryRead(out string foundContent) => _underlyingAccessor.TryRead(out foundContent);

			protected override DataWrapper CreateNewContainer() => new DataWrapper();

			protected override bool Flush(DataWrapper containerToFlush) { _underlyingAccessor.Save(containerToFlush.Contents); return false; }
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
		bool TryRead(out string foundContent);
		void Save(string content);
	}
}
