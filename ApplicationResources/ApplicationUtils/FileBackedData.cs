using System;
using System.IO;
using System.Threading;
using CustomResources.Utils.Concepts;
using CustomResources.Utils.Concepts.DataStructures;
using GeneralUtils = CustomResources.Utils.GeneralUtils.Utils;

namespace ApplicationResources.ApplicationUtils
{
	public class CachedJSONFile<T> : CachedFile<T>
	{
		public CachedJSONFile(string fileName, FileAccessType fileAccessType = FileAccessType.Basic)
			: base(fileName, ApplicationConstants<T>.JSONSerializer.Invert(), fileAccessType)
		{ }
	}

	public class CachedFile<T> : StandardDisposable
	{
		public delegate void OnValueLoadedListener(T loadedValue);
		public delegate void OnValueChangedListener(T previousValue, T newValue);
		public event OnValueLoadedListener OnValueLoaded;
		public event OnValueChangedListener OnValueChanged;

		private readonly Bijection<string, T> _parser;
		private readonly IFileAccessor _fileAccessor;

		private Reference<T> _cachedValue;
		private bool _isLoaded = false;
		private readonly object _loadLock = new object();

		public CachedFile(string fileName, Bijection<string, T> parser, FileAccessType fileAccessType = FileAccessType.Basic)
		{
			_parser = parser;
			_fileAccessor = fileAccessType switch
			{
				FileAccessType.Basic => new BasicFileAccessor(fileName),
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


		private interface IFileAccessor : IDisposable
		{
			bool TryRead(out string foundContent);
			void Save(string content);
		}

		private class BasicFileAccessor : StandardDisposable, IFileAccessor
		{
			private readonly string _fileName;
			internal BasicFileAccessor(string fileName)
			{
				_fileName = fileName;
			}

			public bool TryRead(out string foundContent)
			{
				if (!File.Exists(_fileName))
				{
					foundContent = null;
					return false;
				}
				foundContent = File.ReadAllText(_fileName);
				return true;
			}

			public void Save(string content)
			{
				File.WriteAllTextAsync(_fileName, content);
			}

			protected override void DoDispose()
			{
				// Do Nothing
			}
		}

		public enum FileAccessType
		{
			Basic
		}
	}
}
