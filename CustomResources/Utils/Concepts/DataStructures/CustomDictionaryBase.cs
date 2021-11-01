using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CustomResources.Utils.Extensions;

namespace CustomResources.Utils.Concepts.DataStructures
{
	public abstract class CustomDictionaryBase<K, V> : IInternalDictionary<K, V>
	{
		protected readonly IEqualityComparer<K> _equalityComparer;

		public CustomDictionaryBase(IEqualityComparer<K> equalityComparer = null)
		{
			_equalityComparer = equalityComparer ?? EqualityComparer<K>.Default;
		}

		public IEqualityComparer<K> EqualityComparer => _equalityComparer;

		public bool IsReadOnly => false;

		public abstract bool IsSynchronized { get; }
		public abstract object SyncRoot { get; }
		public abstract int Count { get; }
		public abstract void Add(K key, V value);
		public abstract void Clear();
		public abstract bool Remove(K key);
		public abstract bool TryGetValue(K key, [MaybeNullWhen(false)] out V value);
		public abstract void Update(K key, V value);
		public abstract IEnumerator<KeyValuePair<K, V>> GetEnumerator();
		public abstract V AddOrUpdate(K key, Func<K, V> addValueFactory, Func<K, V, V> updateValueFactory);


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
				AddOrUpdate(key, _ => value, (_, _) => value);
			}
		}

		protected void ThrowKeyAlreadyAddedException(K key) => throw new ArgumentException($"Key {key} already has an entry in the dictionary");
		protected void ThrowNullKeyException() => throw new ArgumentNullException($"Key cannot be null");
	}
}
