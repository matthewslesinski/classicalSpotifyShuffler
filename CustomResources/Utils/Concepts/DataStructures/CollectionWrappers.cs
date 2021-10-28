using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;

namespace CustomResources.Utils.Concepts.DataStructures
{

	#region CollectionWrappers

	public interface IReadOnlyCollectionWrapper<T, CollectionT> where CollectionT : IReadOnlyCollection<T>
	{
		CollectionT WrappedCollection { get; }
	}

	public abstract class ReadOnlyCollectionWrapper<T, WrappedElementT, CollectionT> : IReadOnlyCollection<T>, IReadOnlyCollectionWrapper<WrappedElementT, CollectionT>
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

		public CollectionT WrappedCollection => _wrappedCollection;

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
		where CollectionT : ICollection<WrappedElementT>, IReadOnlyCollection<WrappedElementT>, ICollection
	{
		protected readonly Func<T, WrappedElementT> _mappingFunction;

		public CollectionWrapper(CollectionT wrappedCollection, Bijection<T, WrappedElementT> mappingFunction) : base(wrappedCollection, mappingFunction.Inverse)
		{
			Ensure.ArgumentNotNull(mappingFunction, nameof(mappingFunction));
			_mappingFunction = mappingFunction.Function;
		}

		public override bool IsReadOnly => _wrappedCollection.IsReadOnly;

		public bool IsSynchronized => _wrappedCollection.IsSynchronized;

		public object SyncRoot => _wrappedCollection.SyncRoot;

		public void Add(T item) => _wrappedCollection.Add(_mappingFunction.Invoke(item));

		public void Clear() => _wrappedCollection.Clear();

		public bool Contains(T item) => _wrappedCollection.Contains(_mappingFunction.Invoke(item));

		public bool Remove(T item) => _wrappedCollection.Remove(_mappingFunction.Invoke(item));
	}

	public class CollectionWrapper<T, CollectionT> : CollectionWrapper<T, T, CollectionT> where CollectionT : ICollection<T>, IReadOnlyCollection<T>, ICollection
	{
		public CollectionWrapper(CollectionT wrappedCollection, Bijection<T, T> mappingFunction = null) : base(wrappedCollection, mappingFunction ?? Bijections<T>.Identity) { }
	}

	#endregion

	#region SetWrappers

	public abstract class SetWrapper<T, WrappedElementT, SetT> : CollectionWrapper<T, WrappedElementT, SetT>, ISet<T>, IMappedElementContainer<T, WrappedElementT>
		where SetT : ISet<WrappedElementT>, IReadOnlySet<WrappedElementT>, ICollection
	{
		protected SetWrapper(SetT wrappedCollection, IEqualityComparer<WrappedElementT> wrappedEqualityComparer, Bijection<T, WrappedElementT> mappingFunction)
			: base(wrappedCollection, mappingFunction)
		{
			WrappedEqualityComparer = wrappedEqualityComparer;
		}

		public IEqualityComparer<WrappedElementT> WrappedEqualityComparer { get; }

		public Func<T, WrappedElementT> ElementMapper => _mappingFunction;

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

	#endregion

	#region DictionaryWrappers

	public abstract class DictionaryWrapper<K, V, WrappedK, WrappedV, DictionaryT> : CollectionWrapper<KeyValuePair<K, V>, KeyValuePair<WrappedK, WrappedV>, DictionaryT>,
		IDictionary<K, V>, IReadOnlyDictionary<K, V>, ICollection, IMappedElementContainer<K, WrappedK>
		where DictionaryT : IDictionary<WrappedK, WrappedV>, IReadOnlyDictionary<WrappedK, WrappedV>, ICollection
	{
		protected readonly Bijection<K, WrappedK> _keyMapper;
		protected readonly Bijection<V, WrappedV> _valueMapper;

		// Note that wrappedEqualityComparer should be (equivalent to) the equality comparer used by the wrapped collection
		protected DictionaryWrapper(DictionaryT wrappedCollection, IEqualityComparer<WrappedK> keyEqualityComparer,
			Bijection<K, WrappedK> keyMappingFunction, Bijection<V, WrappedV> valueMappingFunction)
			: base(wrappedCollection, CombineMappingFunctions(keyMappingFunction, valueMappingFunction))
		{
			Ensure.ArgumentNotNull(keyMappingFunction, nameof(keyMappingFunction));
			Ensure.ArgumentNotNull(valueMappingFunction, nameof(valueMappingFunction));
			_keyMapper = keyMappingFunction;
			_valueMapper = valueMappingFunction;
			WrappedEqualityComparer = keyEqualityComparer;
		}

		public ICollection<K> Keys => new KeyCollectionView<K, WrappedK, WrappedV, DictionaryT>(_wrappedCollection, _keyMapper, WrappedEqualityComparer);
		public ICollection<V> Values => new ValueCollectionView<V, WrappedK, WrappedV, DictionaryT>(_wrappedCollection, _valueMapper);
		IEnumerable<K> IReadOnlyDictionary<K, V>.Keys => Keys;
		IEnumerable<V> IReadOnlyDictionary<K, V>.Values => Values;

		public IEqualityComparer<WrappedK> WrappedEqualityComparer { get; }

		public Func<K, WrappedK> ElementMapper => _keyMapper.Function;

		public V this[K key] {
			get => _valueMapper.InvokeInverse(_wrappedCollection.As<IDictionary<WrappedK, WrappedV>>()[_keyMapper.Invoke(key)]);
			set => _wrappedCollection.As<IDictionary<WrappedK, WrappedV>>()[_keyMapper.Invoke(key)] = _valueMapper.Invoke(value);
		}

		public void Add(K key, V value) => _wrappedCollection.Add(_keyMapper.Invoke(key), _valueMapper.Invoke(value));

		public bool Remove(K key) => _wrappedCollection.Remove(_keyMapper.Invoke(key));

		public bool ContainsKey(K key) => _wrappedCollection.As<IDictionary<WrappedK, WrappedV>>().ContainsKey(_keyMapper.Invoke(key));

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

	}

	public class DictionaryWrapper<K, V, DictionaryT> : DictionaryWrapper<K, V, K, V, DictionaryT> where DictionaryT : IDictionary<K, V>, IReadOnlyDictionary<K, V>, ICollection
	{
		public DictionaryWrapper(DictionaryT wrappedCollection, IEqualityComparer<K> equalityComparer) : this(wrappedCollection, equalityComparer, Bijections<K>.Identity, Bijections<V>.Identity) { }
		public DictionaryWrapper(DictionaryT wrappedCollection, IEqualityComparer<K> equalityComparer, Bijection<K, K> keyMappingFunction, Bijection<V, V> valueMappingFunction)
			: base(wrappedCollection, equalityComparer, keyMappingFunction, valueMappingFunction)
		{ }
	}

	#endregion

	#region DictionaryViewWrappers

	public abstract class DictionaryElementView<T, WrappedT, K, V, CollectionT, DictionaryT> : ReadOnlyCollectionWrapper<T, WrappedT, CollectionT>, IInternalCollection<T>
		where CollectionT : ICollection<WrappedT>, IReadOnlyCollection<WrappedT>
		where DictionaryT : IDictionary<K, V>, ICollection, IReadOnlyDictionary<K, V>
	{
		protected readonly DictionaryT _wrappedDictionary;
		public DictionaryElementView(CollectionT elements, DictionaryT wrappedDictionary, Func<WrappedT, T> translationFunc)
			: base(elements, translationFunc)
		{
			_wrappedDictionary = wrappedDictionary;
		}

		public bool IsSynchronized => _wrappedDictionary.IsSynchronized;

		public object SyncRoot => _wrappedDictionary.SyncRoot;

		public void Add(T item) => throw new NotSupportedException(ModifyAttemptErrorMessage);

		public void Clear() => throw new NotSupportedException(ModifyAttemptErrorMessage);

		public abstract bool Contains(T item);

		public bool Remove(T item) => throw new NotSupportedException(ModifyAttemptErrorMessage);

		protected const string ModifyAttemptErrorMessage = "Cannot modify a readonly collection";
	}


	public class KeyCollectionView<T, K, V, DictionaryT> : DictionaryElementView<T, K, K, V, CollectionAsReadOnly<K>, DictionaryT>, IInternalSet<T>, IMappedElementContainer<T, K>
		where DictionaryT : IDictionary<K, V>, ICollection, IReadOnlyDictionary<K, V>
	{
		private readonly IEqualityComparer<K> _equalityComparer;
		private readonly Func<T, K> _mappingRelationship;
		public KeyCollectionView(DictionaryT wrappedDictionary, Bijection<T, K> mappingRelationship, IEqualityComparer<K> equalityComparer = null) : base(new CollectionAsReadOnly<K>(wrappedDictionary.As<IDictionary<K, V>>().Keys), wrappedDictionary, mappingRelationship.Inverse)
		{
			Ensure.ArgumentNotNull(mappingRelationship, nameof(mappingRelationship));
			_equalityComparer = equalityComparer;
			_mappingRelationship = mappingRelationship.Function;
		}

		public IEqualityComparer<K> WrappedEqualityComparer => _equalityComparer;
		public Func<T, K> ElementMapper => _mappingRelationship;

		public new bool Add(T item) => throw new NotSupportedException(ModifyAttemptErrorMessage);

		public override bool Contains(T item) => _wrappedCollection.Contains(_mappingRelationship(item));
	}


	public class KeyCollectionView<K, V, DictionaryT> : KeyCollectionView<K, K, V, DictionaryT> where DictionaryT : IDictionary<K, V>, ICollection, IReadOnlyDictionary<K, V>
	{
		public KeyCollectionView(DictionaryT wrappedDictionary, IEqualityComparer<K> equalityComparer = null)
			: base(wrappedDictionary, Bijections<K>.Identity, equalityComparer ?? EqualityComparer<K>.Default)
		{ }
	}

	public class ValueCollectionView<T, K, V, DictionaryT> : DictionaryElementView<T, V, K, V, CollectionAsReadOnly<V>, DictionaryT>
		where DictionaryT : IDictionary<K, V>, ICollection, IReadOnlyDictionary<K, V>
	{
		public ValueCollectionView(DictionaryT wrappedCollection, Bijection<T, V> bijection) : this(wrappedCollection, bijection.Inverse) { }
		public ValueCollectionView(DictionaryT wrappedDictionary, Func<V, T> translationFunc) : base(new CollectionAsReadOnly<V>(wrappedDictionary.As<IDictionary<K, V>>().Values), wrappedDictionary, translationFunc)
		{
			Ensure.ArgumentNotNull(translationFunc, nameof(translationFunc));
		}

		public override bool Contains(T item) => ContainsValue(item);
		public bool ContainsValue(T item, IEqualityComparer<T> equalityComparer = null)
		{
			equalityComparer ??= EqualityComparer<T>.Default;
			foreach (var value in _wrappedCollection)
			{
				if (equalityComparer.Equals(_translationFunction(value), item))
					return true;
			}
			return false;
		}
	}

	public class ValueCollectionView<K, V, DictionaryT> : ValueCollectionView<V, K, V, DictionaryT>
		where DictionaryT : IDictionary<K, V>, ICollection, IReadOnlyDictionary<K, V>
	{
		public ValueCollectionView(DictionaryT wrappedCollection) : base(wrappedCollection, Bijections<V>.Identity) { }

		public override bool Contains(V item) => _wrappedCollection.Contains(item);
	}

	#endregion
}
