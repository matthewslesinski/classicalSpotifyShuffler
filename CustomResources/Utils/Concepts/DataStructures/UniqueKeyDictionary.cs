using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace SpotifyProject.Utils.Concepts.DataStructures
{
	public class UniqueKeyDictionary<K, V> : IDictionary<K, V>, IInternalCollection<KeyValuePair<K, V>>, IReadOnlyDictionary<K, V>
	{
		private readonly IEqualityComparer<K> _equalityComparer;
		private Node[] _nodes;
		private int _minHash;
		private int _size = 0;

		public UniqueKeyDictionary(IEqualityComparer<K> equalityComparer = null)
		{
			_equalityComparer = equalityComparer ?? EqualityComparer<K>.Default;
			_minHash = int.MaxValue;
			_nodes = Array.Empty<Node>();
		}

		public V this[K key] {
			get
			{
				if (!TryGetValue(key, out var foundValue))
					throw new KeyNotFoundException($"The specified key {key} does not exist in the dictionary");
				return foundValue;
			}
			set
			{
				if (value == null)
					Remove(key);
				else if (ContainsKey(key))
					_nodes[GetIndex(key)] = new Node(key, value);
				else
					Add(key, value);
			}
		}

		IEnumerable<K> IReadOnlyDictionary<K, V>.Keys => Keys;
		public ICollection<K> Keys => new KeyCollectionView(this);

		IEnumerable<V> IReadOnlyDictionary<K, V>.Values => Values;
		public ICollection<V> Values => new ValueCollectionView(this);

		public int Count => _size;

		public bool IsReadOnly => false;

		public void Add(KeyValuePair<K, V> item) => Add(item.Key, item.Value);
		public void Add(K key, V value)
		{
			var nodeToAdd = new Node(key, value);
			if (_size == 0)
			{
				_nodes = new Node[] { nodeToAdd };
				_minHash = _equalityComparer.GetHashCode(key);
				_size += 1;
				return;
			}
			var candidateIndex = GetIndex(key);
			if (candidateIndex < 0)
			{
				var amountToPrepend = Math.Max(-candidateIndex, _nodes.Length);
				var newArr = new Node[_nodes.Length + amountToPrepend];
				Array.Copy(_nodes, 0, newArr, amountToPrepend, _nodes.Length);
				_minHash -= amountToPrepend;
				candidateIndex += amountToPrepend;
			}
			else if (candidateIndex >= _nodes.Length)
			{
				var amountToAppend = Math.Max(candidateIndex - _nodes.Length + 1, _nodes.Length);
				var newArr = new Node[_nodes.Length + amountToAppend];
				Array.Copy(_nodes, 0, newArr, 0, _nodes.Length);
			}
			if (_nodes[candidateIndex] != null)
				throw new ArgumentException($"Key {key} already has an entry in the dictionary");
			_nodes[candidateIndex] = nodeToAdd;
			_size += 1;
		}

		public void Clear()
		{
			_size = 0;
			_minHash = int.MaxValue;
			_nodes = Array.Empty<Node>();
		}

		public bool Contains(KeyValuePair<K, V> item) => TryGetValue(item.Key, out var foundValue) && Equals(foundValue, item.Value);
		public bool ContainsKey(K key) => TryGetValue(key, out _);
		public bool ContainsValue(V value)
		{
			foreach (var kvp in this)
			{
				if (Equals(kvp.Value, value))
					return true;
			}
			return false;
		}

		public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
		{
			var seenSize = 0;
			foreach (var node in _nodes)
			{
				if (node != null)
				{
					yield return node.ToKeyValuePair();
					if (++seenSize >= _size)
						yield break;
				}
			}
		}

		public bool Remove(KeyValuePair<K, V> item) => TryGetValue(item.Key, out var existingValue) && Equals(item.Value, existingValue) && Remove(item.Key);
		public bool Remove(K key)
		{
			var index = GetIndex(key);
			if (index < 0 || index >= _nodes.Length || _nodes[index] == null)
				return false;
			_nodes[index] = null;
			_size -= 1;
			return true;
		}

		public bool TryGetValue(K key, [MaybeNullWhen(false)] out V value)
		{
			var candidateIndex = GetIndex(key);
			Node node;
			if (candidateIndex < 0 || candidateIndex >= _nodes.Length || (node = _nodes[candidateIndex]) == null || !_equalityComparer.Equals(key, node.Key)) {
				value = default;
				return false;
			}
			value = node.Value;
			return true;
		}

		private int GetIndex(K key) => _equalityComparer.GetHashCode(key) - _minHash;

		private class Node
		{
			internal Node(K key, V value)
			{
				Key = key;
				Value = value;
			}

			internal readonly K Key;
			internal readonly V Value;

			internal KeyValuePair<K, V> ToKeyValuePair() => new KeyValuePair<K, V>(Key, Value);
		}

		public abstract class CollectionView<T> : ReadOnlyCollectionWrapper<T, KeyValuePair<K, V>, UniqueKeyDictionary<K, V>>, IInternalCollection<T>
		{
			public CollectionView(UniqueKeyDictionary<K, V> wrappedCollection, Func<KeyValuePair<K, V>, T> translationFunction)
				: base(wrappedCollection, translationFunction)
			{ }

			public void Add(T item) => throw new NotSupportedException("Cannot modify a readonly collection");
			

			public void Clear() => throw new NotSupportedException("Cannot modify a readonly collection");

			public abstract bool Contains(T item);

			public bool Remove(T item) => throw new NotSupportedException("Cannot modify a readonly collection");
		}

		public class KeyCollectionView : CollectionView<K>
		{
			public KeyCollectionView(UniqueKeyDictionary<K, V> wrappedCollection) : base(wrappedCollection, kvp => kvp.Key)
			{ }

			public override bool Contains(K item) => _wrappedCollection.ContainsKey(item);
		}

		public class ValueCollectionView : CollectionView<V>
		{
			public ValueCollectionView(UniqueKeyDictionary<K, V> wrappedCollection) : base(wrappedCollection, kvp => kvp.Value)
			{ }

			public override bool Contains(V item) => _wrappedCollection.ContainsValue(item);
		}
	}

}
