using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CustomResources.Utils.Concepts.DataStructures
{
	public interface IConcurrentDictionary<K, V> : IDictionaryCollection<K, V>
	{
		V AddOrUpdate(K key, V addValue, Func<K, V, V> updateValueFactory);
		V AddOrUpdate(K key, Func<K, V> addValueFactory, Func<K, V, V> updateValueFactory);
		V AddOrUpdate<A>(K key, Func<K, A, V> addValueFactory, Func<K, V, A, V> updateValueFactory, A factoryArgument);
		V GetOrAdd(K key, V value);
		V GetOrAdd(K key, Func<K, V> valueFactory);
		V GetOrAdd<A>(K key, Func<K, A, V> valueFactory, A factoryArgument);
		bool TryAdd(K key, V value);
		bool TryRemove(K key, out V value);
		bool TryRemove(KeyValuePair<K, V> item);
		bool TryUpdate(K key, V newValue, V comparisonValue);
		IReadOnlyDictionary<K, V> GetSnapshot();

		void ICollection<KeyValuePair<K, V>>.CopyTo(KeyValuePair<K, V>[] array, int index) => CopyToImpl(array, index, GetSnapshot());
		void ICollection.CopyTo(Array array, int index) => CopyToImpl(array, index, GetSnapshot());
	}

	public class InternalConcurrentDictionary<K, V> : ConcurrentDictionary<K, V>, IReadOnlyDictionaryCollection<K, V>, IConcurrentDictionary<K, V>
	{
		public InternalConcurrentDictionary(IEqualityComparer<K> keyComparer = null) : base(keyComparer ?? EqualityComparer<K>.Default)
		{
			EqualityComparer = keyComparer ?? EqualityComparer<K>.Default;
		}

		public new KeyCollectionView<K, V, InternalConcurrentDictionary<K, V>> Keys =>
			new KeyCollectionView<K, V, InternalConcurrentDictionary<K, V>>(this, dict => new CollectionAsReadOnly<K>(dict.Keys), EqualityComparer);
		IEnumerable<K> IReadOnlyDictionary<K, V>.Keys => Keys;
		ICollection<K> IDictionary<K, V>.Keys => Keys;

		public IEqualityComparer<K> EqualityComparer { get; }

		IReadOnlyDictionary<K, V> IConcurrentDictionary<K, V>.GetSnapshot() => GetSnapshot();
		public ReadOnlyDictionary<K, V> GetSnapshot() => new ReadOnlyDictionary<K, V>(new Dictionary<K, V>(ToArray()));
	}
}
