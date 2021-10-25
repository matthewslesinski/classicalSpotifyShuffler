using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CustomResources.Utils.GeneralUtils;

namespace CustomResources.Utils.Concepts.DataStructures
{
	public class UniqueKeyDictionary<K, V> : CustomDictionaryBase<K, V>
	{
		private Node[] _nodes;
		private int _minHash;

		public UniqueKeyDictionary(IEqualityComparer<K> equalityComparer = null) : base(equalityComparer)
		{
			_minHash = int.MaxValue;
			_nodes = Array.Empty<Node>();
		}

		public override void Add(K key, V value)
		{
			Ensure.ArgumentNotNull(key, nameof(key));
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
				_nodes = newArr;
			}
			else if (candidateIndex >= _nodes.Length)
			{
				var amountToAppend = Math.Max(candidateIndex - _nodes.Length + 1, _nodes.Length);
				var newArr = new Node[_nodes.Length + amountToAppend];
				Array.Copy(_nodes, 0, newArr, 0, _nodes.Length);
				_nodes = newArr;
			}
			if (_nodes[candidateIndex] != null)
				ThrowKeyAlreadyAddedException(key);
			_nodes[candidateIndex] = nodeToAdd;
			_size += 1;
		}

		public override void Clear()
		{
			_size = 0;
			_minHash = int.MaxValue;
			_nodes = Array.Empty<Node>();
		}

		public override IEnumerator<KeyValuePair<K, V>> GetEnumerator()
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

		public override bool Remove(K key)
		{
			Ensure.ArgumentNotNull(key, nameof(key));

			var index = GetIndex(key);
			if (index < 0 || index >= _nodes.Length || _nodes[index] == null)
				return false;
			_nodes[index] = null;
			_size -= 1;
			return true;
		}

		public override bool TryGetValue(K key, [MaybeNullWhen(false)] out V value)
		{
			Ensure.ArgumentNotNull(key, nameof(key));

			var candidateIndex = GetIndex(key);
			Node node;
			if (candidateIndex < 0 || candidateIndex >= _nodes.Length || (node = _nodes[candidateIndex]) == null || !_equalityComparer.Equals(key, node.Key)) {
				value = default;
				return false;
			}
			value = node.Value;
			return true;
		}

		public override void Update(K key, V value) => _nodes[GetIndex(key.EnsureNotNull(nameof(key)))] = new Node(key, value);

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
	}
}
