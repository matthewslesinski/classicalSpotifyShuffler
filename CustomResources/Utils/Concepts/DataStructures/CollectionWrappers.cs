using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;

namespace CustomResources.Utils.Concepts.DataStructures
{
	public abstract class ReadOnlyCollectionWrapper<T, WrappedElementT, CollectionT> : IReadOnlyCollection<T>
		where CollectionT : IReadOnlyCollection<WrappedElementT>
	{
		protected readonly CollectionT _wrappedCollection;
		protected readonly Func<WrappedElementT, T> _translationFunction;

		protected ReadOnlyCollectionWrapper(CollectionT wrappedCollection, Func<WrappedElementT, T> translationFunction)
		{
			Ensure.ArgumentNotNull(wrappedCollection, nameof(wrappedCollection));
			Ensure.ArgumentNotNull(translationFunction, nameof(translationFunction));

			_wrappedCollection = wrappedCollection;
			_translationFunction = translationFunction;
		}

		public virtual int Count => _wrappedCollection.Count;

		public virtual bool IsReadOnly => true;

		public virtual IEnumerator<T> GetEnumerator()
		{
			foreach (var wrappedElement in _wrappedCollection)
				yield return _translationFunction(wrappedElement);
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public override string ToString() => "{" + string.Join(", ", this) + "}";
	}

	public class CollectionWrapper<T, WrappedElementT, CollectionT> : ReadOnlyCollectionWrapper<T, WrappedElementT, CollectionT>, IInternalCollection<T>
		where CollectionT : ICollection<WrappedElementT>, IReadOnlyCollection<WrappedElementT>
	{
		protected readonly Func<T, WrappedElementT> _mappingFunction;

		public CollectionWrapper(CollectionT wrappedCollection, Bijection<T, WrappedElementT> mappingFunction) : base(wrappedCollection, mappingFunction.Inverse)
		{
			Ensure.ArgumentNotNull(mappingFunction, nameof(mappingFunction));
			_mappingFunction = mappingFunction.Function;
		}

		public override bool IsReadOnly => _wrappedCollection.IsReadOnly;

		public void Add(T item) => _wrappedCollection.Add(_mappingFunction.Invoke(item));

		public void Clear() => _wrappedCollection.Clear();

		public bool Contains(T item) => _wrappedCollection.Contains(_mappingFunction.Invoke(item));

		public bool Remove(T item) => _wrappedCollection.Remove(_mappingFunction.Invoke(item));
	}

	public class CollectionWrapper<T, CollectionT> : CollectionWrapper<T, T, CollectionT> where CollectionT : ICollection<T>, IReadOnlyCollection<T>
	{
		public CollectionWrapper(CollectionT wrappedCollection, Bijection<T, T> mappingFunction = null) : base(wrappedCollection, mappingFunction ?? Bijections<T>.Identity) { }
	}

	public abstract class SetWrapper<T, WrappedElementT, SetT> : CollectionWrapper<T, WrappedElementT, SetT>, ISet<T>
		where SetT : ISet<WrappedElementT>, IReadOnlySet<WrappedElementT>
	{
		protected SetWrapper(SetT wrappedCollection, Bijection<T, WrappedElementT> mappingFunction)
			: base(wrappedCollection, mappingFunction)
		{
		}

		void ICollection<T>.Add(T item) => Add(item);

		public new bool Add(T item) => _wrappedCollection.Add(_mappingFunction.Invoke(item));

		public void ExceptWith(IEnumerable<T> other) => _wrappedCollection.ExceptWith(MapOtherIEnumerable(other));

		public void IntersectWith(IEnumerable<T> other) => _wrappedCollection.IntersectWith(MapOtherIEnumerable(other));

		public bool IsProperSubsetOf(IEnumerable<T> other) => _wrappedCollection.As<ISet<WrappedElementT>>().IsProperSubsetOf(MapOtherIEnumerable(other));

		public bool IsProperSupersetOf(IEnumerable<T> other) => _wrappedCollection.As<ISet<WrappedElementT>>().IsProperSupersetOf(MapOtherIEnumerable(other));

		public bool IsSubsetOf(IEnumerable<T> other) => _wrappedCollection.As<ISet<WrappedElementT>>().IsSubsetOf(MapOtherIEnumerable(other));

		public bool IsSupersetOf(IEnumerable<T> other) => _wrappedCollection.As<ISet<WrappedElementT>>().IsSupersetOf(MapOtherIEnumerable(other));

		public bool Overlaps(IEnumerable<T> other) => _wrappedCollection.As<ISet<WrappedElementT>>().Overlaps(MapOtherIEnumerable(other));

		public bool SetEquals(IEnumerable<T> other) => _wrappedCollection.As<ISet<WrappedElementT>>().SetEquals(MapOtherIEnumerable(other));

		public void SymmetricExceptWith(IEnumerable<T> other) => _wrappedCollection.SymmetricExceptWith(MapOtherIEnumerable(other));

		public void UnionWith(IEnumerable<T> other) => _wrappedCollection.UnionWith(MapOtherIEnumerable(other));

		private IEnumerable<WrappedElementT> MapOtherIEnumerable(IEnumerable<T> other) => other is SetWrapper<T, WrappedElementT, SetT> otherWrapper
			? otherWrapper._wrappedCollection
			: other.Select(_mappingFunction);
	}

	public abstract class DictionaryWrapper<K, V, WrappedK, WrappedV, DictionaryT> : CollectionWrapper<KeyValuePair<K, V>, KeyValuePair<WrappedK, WrappedV>, DictionaryT>, IDictionary<K, V>, IReadOnlyDictionary<K, V>
		where DictionaryT : IDictionary<WrappedK, WrappedV>, IReadOnlyDictionary<WrappedK, WrappedV>
	{
		protected readonly Bijection<K, WrappedK> _keyMapper;
		protected readonly Bijection<V, WrappedV> _valueMapper;

		// Note that wrappedEqualityComparer should be (equivalent to) the equality comparer used by the wrapped collection
		protected DictionaryWrapper(DictionaryT wrappedCollection, Bijection<K, WrappedK> keyMappingFunction, Bijection<V, WrappedV> valueMappingFunction) : base(wrappedCollection, CombineMappingFunctions(keyMappingFunction, valueMappingFunction))
		{
			Ensure.ArgumentNotNull(keyMappingFunction, nameof(keyMappingFunction));
			Ensure.ArgumentNotNull(valueMappingFunction, nameof(valueMappingFunction));
			_keyMapper = keyMappingFunction;
			_valueMapper = valueMappingFunction;
		}

		public ICollection<K> Keys => new CollectionWrapper<K, WrappedK, CollectionAsReadOnly<WrappedK>>(new CollectionAsReadOnly<WrappedK>(_wrappedCollection.As<IDictionary<WrappedK, WrappedV>>().Keys), _keyMapper);
		public ICollection<V> Values => new CollectionWrapper<V, WrappedV, CollectionAsReadOnly<WrappedV>>(new CollectionAsReadOnly<WrappedV>(_wrappedCollection.As<IDictionary<WrappedK, WrappedV>>().Values), _valueMapper);
		IEnumerable<K> IReadOnlyDictionary<K, V>.Keys => Keys;
		IEnumerable<V> IReadOnlyDictionary<K, V>.Values => Values;

		public V this[K key] {
			get => _valueMapper.InvokeInverse(_wrappedCollection.As<IDictionary<WrappedK, WrappedV>>()[_keyMapper.Invoke(key)]);
			set => _wrappedCollection.As<IDictionary<WrappedK, WrappedV>>()[_keyMapper.Invoke(key)] = _valueMapper.Invoke(value);
		}

		public void Add(K key, V value) => _wrappedCollection.Add(_keyMapper.Invoke(key), _valueMapper.Invoke(value));

		public bool Remove(K key) => _wrappedCollection.Remove(_keyMapper.Invoke(key));

		public bool TryGetValue(K key, [MaybeNullWhen(false)] out V value)
		{
			var didFindValue = _wrappedCollection.As<IDictionary<WrappedK, WrappedV>>().TryGetValue(_keyMapper.Invoke(key), out var foundValue);
			value = _valueMapper.InvokeInverse(foundValue);
			return didFindValue;
		}

		private static Bijection<KeyValuePair<K, V>, KeyValuePair<WrappedK, WrappedV>> CombineMappingFunctions(Bijection<K, WrappedK> keyMappingFunction, Bijection<V, WrappedV> valueMappingFunction) =>
			new Bijection<KeyValuePair<K, V>, KeyValuePair<WrappedK, WrappedV>>(
				originalPair => new KeyValuePair<WrappedK, WrappedV>(keyMappingFunction.Invoke(originalPair.Key), valueMappingFunction.Invoke(originalPair.Value)),
				wrappedPair => new KeyValuePair<K, V>(keyMappingFunction.InvokeInverse(wrappedPair.Key), valueMappingFunction.InvokeInverse(wrappedPair.Value)));

		public bool ContainsKey(K key) => _wrappedCollection.As<IDictionary<WrappedK, WrappedV>>().ContainsKey(_keyMapper.Invoke(key));
	}

	public class DictionaryWrapper<K, V, DictionaryT> : DictionaryWrapper<K, V, K, V, DictionaryT> where DictionaryT : IDictionary<K, V>, IReadOnlyDictionary<K, V>
	{
		public DictionaryWrapper(DictionaryT wrappedCollection) : this(wrappedCollection, Bijections<K>.Identity, Bijections<V>.Identity) { }
		public DictionaryWrapper(DictionaryT wrappedCollection, Bijection<K, K> keyMappingFunction, Bijection<V, V> valueMappingFunction) : base(wrappedCollection, keyMappingFunction, valueMappingFunction) { }
	}
}
