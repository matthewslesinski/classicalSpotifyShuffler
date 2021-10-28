using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CustomResources.Utils.Extensions;

namespace CustomResources.Utils.Concepts.DataStructures
{
	public abstract class CustomDictionaryBase<K, V> : IInternalDictionary<K, V>
	{
		protected readonly IEqualityComparer<K> _equalityComparer;
		protected int _size = 0;

		public CustomDictionaryBase(IEqualityComparer<K> equalityComparer)
		{
			_equalityComparer = equalityComparer ?? EqualityComparer<K>.Default;
		}

		public IEqualityComparer<K> EqualityComparer => _equalityComparer;

		public int Count => _size;

		public bool IsReadOnly => false;

		public abstract bool IsSynchronized { get; }
		public abstract object SyncRoot { get; }

		public abstract void Add(K key, V value);
		public abstract void Clear();
		public abstract bool Remove(K key);
		public abstract bool TryGetValue(K key, [MaybeNullWhen(false)] out V value);
		public abstract void Update(K key, V value);
		public abstract IEnumerator<KeyValuePair<K, V>> GetEnumerator();

		public V this[K key]
		{
			get
			{
				if (!TryGetValue(key, out var foundValue))
					throw new KeyNotFoundException($"The specified key, {key}, does not exist in the dictionary");
				return foundValue;
			}
			set
			{
				if (value == null)
					Remove(key);
				else if (TryGetValue(key, out _))
					Update(key, value);
				else
					Add(key, value);
			}
		}

		protected void ThrowKeyAlreadyAddedException(K key) => throw new ArgumentException($"Key {key} already has an entry in the dictionary");
		protected void ThrowNullKeyException() => throw new ArgumentNullException($"Key cannot be null");
	}
}
