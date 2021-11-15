using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;

namespace CustomResources.Utils.Concepts.DataStructures
{

	#region CollectionWrappers

	public interface IWrapper<out ObjectT>
	{
		ObjectT WrappedObject { get; }
	}

	public interface IWrappedElementContainer<T, out WrappedT> : IElementContainer<T>, IWrapper<WrappedT>
		where WrappedT : IElementContainer<T>
	{
		IEqualityComparer<T> IElementContainer<T>.EqualityComparer => WrappedObject.EqualityComparer;
	}

	public interface ICollectionWrapper<T, CollectionT> : IWrapper<CollectionT>, ICollection where CollectionT : IReadOnlyCollection<T>, ICollection<T>, ICollection
	{
		bool IsReadOnly => WrappedObject.IsReadOnly;

		bool ICollection.IsSynchronized => WrappedObject.IsSynchronized;

		object ICollection.SyncRoot => WrappedObject.SyncRoot;
	}

	public class ReadOnlyCollectionWrapper<T, WrappedElementT, CollectionT> : IReadOnlyCollection<T>, IWrapper<CollectionT>
		where CollectionT : IReadOnlyCollection<WrappedElementT>
	{
		protected readonly CollectionT _wrappedCollection;
		protected readonly Func<WrappedElementT, T> _translationFunction;

		public ReadOnlyCollectionWrapper(CollectionT wrappedCollection, Func<WrappedElementT, T> translationFunction)
		{
			Ensure.ArgumentNotNull(wrappedCollection, nameof(wrappedCollection));
			Ensure.ArgumentNotNull(translationFunction, nameof(translationFunction));

			_wrappedCollection = wrappedCollection;
			_translationFunction = translationFunction;
		}

		public CollectionT WrappedObject => _wrappedCollection;

		public int Count => _wrappedCollection.Count;

		public IEnumerator<T> GetEnumerator()
		{
			foreach (var wrappedElement in _wrappedCollection)
				yield return _translationFunction(wrappedElement);
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		protected internal bool ContainsValue(T item, IEqualityComparer<T> equalityComparer = null)
		{
			equalityComparer ??= EqualityComparer<T>.Default;
			foreach (var value in _wrappedCollection)
			{
				if (equalityComparer.Equals(_translationFunction(value), item))
					return true;
			}
			return false;
		}

		public override string ToString() => "{" + string.Join(", ", this) + "}";
	}

	public class ReadOnlyCollectionWrapper<T, WrappedT> : ReadOnlyCollectionWrapper<T, WrappedT, IReadOnlyCollection<WrappedT>>
	{
		public ReadOnlyCollectionWrapper(IReadOnlyCollection<WrappedT> wrappedCollection, Func<WrappedT, T> translationFunction)
			: base(wrappedCollection, translationFunction)
		{ }
	}

	public class CollectionWrapper<T, WrappedElementT, CollectionT> : ReadOnlyCollectionWrapper<T, WrappedElementT, CollectionT>,
		IGenericInternalCollection<T>, IInternalReadOnlyCollection<T>, ICollectionWrapper<WrappedElementT, CollectionT>
		where CollectionT : ICollection<WrappedElementT>, IReadOnlyCollection<WrappedElementT>, ICollection
	{
		protected readonly Func<T, WrappedElementT> _mappingFunction;

		public CollectionWrapper(CollectionT wrappedCollection, Bijection<T, WrappedElementT> mappingFunction) : base(wrappedCollection, mappingFunction.Inverse)
		{
			Ensure.ArgumentNotNull(mappingFunction, nameof(mappingFunction));
			_mappingFunction = mappingFunction.Function;
		}

		public bool IsReadOnly => _wrappedCollection.IsReadOnly;

		public void Add(T item) => _wrappedCollection.Add(_mappingFunction.Invoke(item));

		public void Clear() => _wrappedCollection.Clear();

		public bool Contains(T item) => _wrappedCollection.Contains(_mappingFunction.Invoke(item));

		public bool Remove(T item) => _wrappedCollection.Remove(_mappingFunction.Invoke(item));
	}

	public class CollectionWrapper<T, CollectionT> : CollectionWrapper<T, T, CollectionT> where CollectionT : ICollection<T>, IReadOnlyCollection<T>, ICollection
	{
		public CollectionWrapper(CollectionT wrappedCollection, Bijection<T, T> mappingFunction = null)
			: base(wrappedCollection, mappingFunction ?? Bijections<T>.Identity)
		{ }
	}

	#endregion

	#region SetWrappers

	public class SetWrapper<T, WrappedElementT, SetT> : CollectionWrapper<T, WrappedElementT, SetT>, ISet<T>, IMappedElementContainer<T, WrappedElementT>
		where SetT : ISet<WrappedElementT>, IReadOnlySet<WrappedElementT>, ICollection
	{
		public SetWrapper(SetT wrappedCollection, Bijection<T, WrappedElementT> mappingFunction, IEqualityComparer<WrappedElementT> wrappedEqualityComparer = null)
			: base(wrappedCollection, mappingFunction)
		{
			WrappedEqualityComparer = wrappedEqualityComparer ?? EqualityComparer<WrappedElementT>.Default;
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

	public class ReadOnlyDictionaryWrapper<K, V, WrappedK, WrappedV, DictionaryT> : ReadOnlyCollectionWrapper<KeyValuePair<K, V>, KeyValuePair<WrappedK, WrappedV>, DictionaryT>,
		IReadOnlyDictionaryCollection<K, V>, IMappedElementContainer<K, WrappedK>, IWrapper<DictionaryT>
		where DictionaryT : IReadOnlyDictionary<WrappedK, WrappedV>, ICollection
	{
		protected readonly Bijection<K, WrappedK> _keyMapper;
		protected readonly Func<WrappedV, V> _valueTranslationFunc;

		// Note that wrappedEqualityComparer should be (equivalent to) the equality comparer used by the wrapped collection
		public ReadOnlyDictionaryWrapper(DictionaryT wrappedCollection, Bijection<K, WrappedK> keyMappingFunction,
			Func<WrappedV, V> valueTranslationFunction, IEqualityComparer<WrappedK> keyEqualityComparer = null)
			: base(wrappedCollection, CombineMappingFunctions(keyMappingFunction.Inverse, valueTranslationFunction))
		{
			Ensure.ArgumentNotNull(keyMappingFunction, nameof(keyMappingFunction));
			Ensure.ArgumentNotNull(valueTranslationFunction, nameof(valueTranslationFunction));
			_keyMapper = keyMappingFunction;
			_valueTranslationFunc = valueTranslationFunction;
			WrappedEqualityComparer = keyEqualityComparer ?? EqualityComparer<WrappedK>.Default;
		}

		public IEnumerable<K> Keys
		{
			get
			{
				foreach (var wrappedKey in _wrappedCollection.Keys)
					yield return _keyMapper.Inverse(wrappedKey);
			}
		}
		public IEnumerable<V> Values
		{
			get
			{
				foreach (var wrappedValue in _wrappedCollection.Values)
					yield return _valueTranslationFunc(wrappedValue);
			}
		}

		bool ICollection.IsSynchronized => _wrappedCollection.IsSynchronized;

		object ICollection.SyncRoot => _wrappedCollection.SyncRoot;

		public IEqualityComparer<WrappedK> WrappedEqualityComparer { get; }

		public Func<K, WrappedK> ElementMapper => _keyMapper.Function;

		public V this[K key] => _valueTranslationFunc(_wrappedCollection.As<IReadOnlyDictionary<WrappedK, WrappedV>>()[_keyMapper.Invoke(key)]);

		public bool ContainsKey(K key) => _wrappedCollection.As<IReadOnlyDictionary<WrappedK, WrappedV>>().ContainsKey(_keyMapper.Invoke(key));

		public bool TryGetValue(K key, [MaybeNullWhen(false)] out V value)
		{
			var didFindValue = _wrappedCollection.TryGetValue(_keyMapper.Invoke(key), out var foundValue);
			value = _valueTranslationFunc(foundValue);
			return didFindValue;
		}

		protected static Func<KeyValuePair<K1, V1>, KeyValuePair<K2, V2>> CombineMappingFunctions<K1, K2, V1, V2>(Func<K1, K2> keyMappingFunction, Func<V1, V2> valueMappingFunction) =>
			originalPair => new KeyValuePair<K2, V2>(keyMappingFunction(originalPair.Key), valueMappingFunction(originalPair.Value));
	}

	public class ReadOnlyDictionaryWrapper<K, V, DictionaryT> : ReadOnlyDictionaryWrapper<K, V, K, V, DictionaryT> where DictionaryT : IReadOnlyDictionary<K, V>, ICollection
	{
		public ReadOnlyDictionaryWrapper(DictionaryT wrappedCollection, Bijection<K, K> keyMappingFunction = null, Func<V, V> valueMappingFunction = null, IEqualityComparer<K> equalityComparer = null)
			: base(wrappedCollection, keyMappingFunction ?? Bijections<K>.Identity, valueMappingFunction ?? Bijections<V>.Identity.Inverse, equalityComparer)
		{ }
	}

	public class DictionaryWrapper<K, V, WrappedK, WrappedV, DictionaryT> : ReadOnlyDictionaryWrapper<K, V, WrappedK, WrappedV, DictionaryT>,
		IDictionaryCollection<K, V>, IReadOnlyDictionary<K, V>, IMappedElementContainer<K, WrappedK>, IGenericInternalCollection<KeyValuePair<K, V>>
		where DictionaryT : IDictionary<WrappedK, WrappedV>, IReadOnlyDictionary<WrappedK, WrappedV>, ICollection
	{
		protected readonly Func<V, WrappedV> _valueMapper;
		protected readonly Func<KeyValuePair<K, V>, KeyValuePair<WrappedK, WrappedV>> _kvpMappingFunction;

		// Note that wrappedEqualityComparer should be (equivalent to) the equality comparer used by the wrapped collection
		public DictionaryWrapper(DictionaryT wrappedCollection, Bijection<K, WrappedK> keyMappingFunction,
			Bijection<V, WrappedV> valueMappingFunction, IEqualityComparer<WrappedK> keyEqualityComparer = null)
			: base(wrappedCollection, keyMappingFunction, valueMappingFunction.EnsureNotNull(nameof(valueMappingFunction)).Inverse, keyEqualityComparer)
		{
			_valueMapper = valueMappingFunction.Function;
			_kvpMappingFunction = CombineMappingFunctions(keyMappingFunction.Function, valueMappingFunction.Function);
		}

		public new KeyCollectionView<K, WrappedK, WrappedV, DictionaryT> Keys => new KeyCollectionView<K, WrappedK, WrappedV, DictionaryT>(_wrappedCollection,
			dict => dict.As<IDictionary<WrappedK, WrappedV>>().Keys.AsReadOnly(),
			_keyMapper,
			WrappedEqualityComparer);
		public new ValueCollectionView<V, WrappedK, WrappedV, DictionaryT> Values => new ValueCollectionView<V, WrappedK, WrappedV, DictionaryT>(_wrappedCollection,
			dict => dict.As<IDictionary<WrappedK, WrappedV>>().Values.AsReadOnly(),
			_valueTranslationFunc);
		IEnumerable<K> IReadOnlyDictionary<K, V>.Keys => Keys;
		IEnumerable<V> IReadOnlyDictionary<K, V>.Values => Values;
		ICollection<K> IDictionary<K, V>.Keys => Keys;
		ICollection<V> IDictionary<K, V>.Values => Values;

		public bool IsReadOnly => _wrappedCollection.IsReadOnly;

		public new V this[K key] {
			get => base[key];
			set => _wrappedCollection.As<IDictionary<WrappedK, WrappedV>>()[_keyMapper.Invoke(key)] = _valueMapper.Invoke(value);
		}

		public void Add(K key, V value) => _wrappedCollection.Add(_keyMapper.Invoke(key), _valueMapper.Invoke(value));
		public void Add(KeyValuePair<K, V> item) => _wrappedCollection.Add(_kvpMappingFunction(item));
		public void Clear() => _wrappedCollection.Clear();
		public bool Contains(KeyValuePair<K, V> item) => _wrappedCollection.Contains(_kvpMappingFunction(item));
		public bool Remove(K key) => _wrappedCollection.Remove(_keyMapper.Invoke(key));
		public bool Remove(KeyValuePair<K, V> item) => _wrappedCollection.Remove(_kvpMappingFunction(item));
	}

	public class DictionaryWrapper<K, V, DictionaryT> : DictionaryWrapper<K, V, K, V, DictionaryT> where DictionaryT : IDictionary<K, V>, IReadOnlyDictionary<K, V>, ICollection
	{
		public DictionaryWrapper(DictionaryT wrappedCollection, Bijection<K, K> keyMappingFunction = null, Bijection<V, V> valueMappingFunction = null, IEqualityComparer<K> equalityComparer = null)
			: base(wrappedCollection, keyMappingFunction ?? Bijections<K>.Identity, valueMappingFunction ?? Bijections<V>.Identity, equalityComparer)
		{ }
	}

	#endregion

	#region DictionaryViewWrappers

	public abstract class DictionaryElementView<T, WrappedT, K, V, CollectionT, DictionaryT> : ReadOnlyCollectionWrapper<T, WrappedT, CollectionT>,
		IInternalCollection<T>
		where CollectionT : IReadOnlyCollection<WrappedT>
		where DictionaryT : ICollection, IReadOnlyDictionary<K, V>
	{
		protected readonly DictionaryT _wrappedDictionary;
		public DictionaryElementView(CollectionT elements, DictionaryT wrappedDictionary, Func<WrappedT, T> translationFunc)
			: base(elements, translationFunc)
		{
			_wrappedDictionary = wrappedDictionary;
		}

		public bool IsSynchronized => _wrappedDictionary.IsSynchronized;

		public object SyncRoot => _wrappedDictionary.SyncRoot;

		public bool IsReadOnly => true;

		public void Add(T item) => throw new NotSupportedException(ModifyAttemptErrorMessage);

		public void Clear() => throw new NotSupportedException(ModifyAttemptErrorMessage);

		public abstract bool Contains(T item);

		public bool Remove(T item) => throw new NotSupportedException(ModifyAttemptErrorMessage);

		protected const string ModifyAttemptErrorMessage = "Cannot modify a readonly collection";
	}

	public class KeyCollectionView<T, K, V, DictionaryT> : DictionaryElementView<T, K, K, V, IGenericInternalCollection<K>, DictionaryT>, IInternalSet<T>, IMappedElementContainer<T, K>
		where DictionaryT : ICollection, IReadOnlyDictionary<K, V>
	{
		private readonly IEqualityComparer<K> _equalityComparer;
		private readonly Func<T, K> _mappingRelationship;

		public KeyCollectionView(DictionaryT wrappedDictionary, Bijection<T, K> mappingRelationship, IEqualityComparer<K> equalityComparer = null)
			: this(wrappedDictionary,
				  dict => new ReadOnlyAsFullCollection<K>(new ReadOnlyCollectionWrapper<K, KeyValuePair<K, V>>(dict, kvp => kvp.Key), wrappedDictionary.As<IReadOnlyDictionary<K, V>>().ContainsKey),
				  mappingRelationship,
				  equalityComparer) { }

		public KeyCollectionView(DictionaryT wrappedDictionary, Func<DictionaryT, IGenericInternalCollection<K>> keyCollectionRetriever, Bijection<T, K> mappingRelationship, IEqualityComparer<K> equalityComparer = null)
			: base(keyCollectionRetriever.EnsureNotNull(nameof(keyCollectionRetriever))(wrappedDictionary), wrappedDictionary, mappingRelationship.Inverse)
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

	public class KeyCollectionView<K, V, DictionaryT> : KeyCollectionView<K, K, V, DictionaryT> where DictionaryT : ICollection, IReadOnlyDictionary<K, V>
	{
		public KeyCollectionView(DictionaryT wrappedDictionary, IEqualityComparer<K> equalityComparer = null)
			: base(wrappedDictionary, Bijections<K>.Identity, equalityComparer ?? EqualityComparer<K>.Default)
		{ }

		public KeyCollectionView(DictionaryT wrappedDictionary, Func<DictionaryT, IGenericInternalCollection<K>> keyCollectionRetriever, IEqualityComparer<K> equalityComparer = null)
			: base(wrappedDictionary, keyCollectionRetriever, Bijections<K>.Identity, equalityComparer ?? EqualityComparer<K>.Default)
		{ }
	}

	public class ValueCollectionView<T, K, V, DictionaryT> : DictionaryElementView<T, V, K, V, IGenericInternalCollection<V>, DictionaryT>
		where DictionaryT : ICollection, IReadOnlyDictionary<K, V>
	{
		public ValueCollectionView(DictionaryT wrappedCollection, Bijection<T, V> bijection) : this(wrappedCollection, bijection.Inverse) { }
		public ValueCollectionView(DictionaryT wrappedCollection, Func<DictionaryT, IGenericInternalCollection<V>> valueCollectionRetriever, Bijection<T, V> bijection)
			: this(wrappedCollection, valueCollectionRetriever, bijection.Inverse)
		{ }

		public ValueCollectionView(DictionaryT wrappedDictionary, Func<V, T> translationFunc)
			: this(wrappedDictionary, BuildWrappedCollectionFromDictionaryEnumerator, translationFunc)
		{ }

		public ValueCollectionView(DictionaryT wrappedDictionary, Func<DictionaryT, IGenericInternalCollection<V>> valueCollectionRetriever, Func<V, T> translationFunc)
			: base(valueCollectionRetriever.EnsureNotNull(nameof(valueCollectionRetriever))(wrappedDictionary), wrappedDictionary, translationFunc)
		{
			Ensure.ArgumentNotNull(translationFunc, nameof(translationFunc));
		}

		public override bool Contains(T item) => ContainsValue(item);

		private static IGenericInternalCollection<V> BuildWrappedCollectionFromDictionaryEnumerator(DictionaryT dict)
		{
			var dictValues = new ReadOnlyCollectionWrapper<V, KeyValuePair<K, V>>(dict, kvp => kvp.Value);
			return new ReadOnlyAsFullCollection<V>(dictValues, item => dictValues.ContainsValue(item));
		} 
	}

	public class ValueCollectionView<K, V, DictionaryT> : ValueCollectionView<V, K, V, DictionaryT>
		where DictionaryT : ICollection, IReadOnlyDictionary<K, V>
	{
		public ValueCollectionView(DictionaryT wrappedCollection) : base(wrappedCollection, Bijections<V>.Identity) { }
		public ValueCollectionView(DictionaryT wrappedDictionary, Func<DictionaryT, IGenericInternalCollection<V>> valueCollectionRetriever)
			: base(wrappedDictionary, valueCollectionRetriever, Bijections<V>.Identity)
		{ }

		public override bool Contains(V item) => _wrappedCollection.Contains(item);
	}

	#endregion

	#region Collection Filters

	public class ReadOnlyCollectionFilter<T, CollectionT> : IReadOnlyCollection<T>, IWrapper<CollectionT>
		where CollectionT : IReadOnlyCollection<T>
	{
		protected readonly CollectionT _wrappedCollection;
		protected readonly Func<T, bool> _filter;

		private int? _size = null; 

		public ReadOnlyCollectionFilter(CollectionT wrappedCollection, Func<T, bool> filter)
		{
			Ensure.ArgumentNotNull(wrappedCollection, nameof(wrappedCollection));
			Ensure.ArgumentNotNull(filter, nameof(filter));

			_wrappedCollection = wrappedCollection;
			_filter = filter;
		}

		public CollectionT WrappedObject => _wrappedCollection;

		public int Count
		{
			get
			{
				if (!_size.HasValue)
				{
					var count = this.Select(_ => 1).Sum();
					_size = count;
					return count;
				}
				return _size.Value;
			}
		}

		public IEnumerator<T> GetEnumerator()
		{
			foreach (var wrappedElement in _wrappedCollection)
				if (_filter(wrappedElement))
					yield return wrappedElement;
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public override string ToString() => "{" + string.Join(", ", this) + "}";
	}

	public class ReadOnlyDictionaryFilter<K, V, DictionaryT> : ReadOnlyCollectionFilter<KeyValuePair<K, V>, DictionaryT>,
		IReadOnlyDictionary<K, V>, IElementContainer<K>, IReadOnlyDictionaryCollection<K, V>
		where DictionaryT : IReadOnlyDictionary<K, V>, ICollection
	{
		protected readonly Func<K, bool> _keyFilter;
		protected readonly Func<V, bool> _valueFilter;

		// Note that wrappedEqualityComparer should be (equivalent to) the equality comparer used by the wrapped collection
		public ReadOnlyDictionaryFilter(DictionaryT wrappedCollection, Func<K, bool> keyFilter = null, Func<V, bool> valueFilter = null, IEqualityComparer<K> keyEqualityComparer = null)
			: base(wrappedCollection, kvp => (keyFilter == null || keyFilter(kvp.Key)) && (valueFilter == null || valueFilter(kvp.Value)))
		{
			if (keyFilter == null && valueFilter == null)
				throw new AggregateException(new ArgumentNullException(nameof(keyFilter)), new ArgumentNullException(nameof(valueFilter)));
			_keyFilter = keyFilter ?? (_ => true);
			_valueFilter = valueFilter ?? (_ => true);
			EqualityComparer = keyEqualityComparer ?? EqualityComparer<K>.Default;
		}

		public IEnumerable<K> Keys
		{
			get
			{
				foreach(var key in _wrappedCollection.Keys)
				{
					if (_keyFilter(key))
						yield return key;
				}
			}
		}
		public IEnumerable<V> Values
		{
			get
			{
				foreach(var value in _wrappedCollection.Values)
				{
					if (_valueFilter(value))
						yield return value;
				}
			}
		}

		public IEqualityComparer<K> EqualityComparer { get; }

		public bool IsSynchronized => _wrappedCollection.IsSynchronized;

		public object SyncRoot => _wrappedCollection.SyncRoot;

		public V this[K key]
		{
			get
			{
				if (!_keyFilter(key))
					throw new KeyNotFoundException($"The given key is not in this dictionary because it is filtered out: {key}");
				var foundValue = _wrappedCollection[key];
				if (!_valueFilter(foundValue))
					throw new KeyNotFoundException($"The found value is not in this dictionary because it is filtered out: {foundValue}");
				return foundValue;
			}
		}

		public bool ContainsKey(K key) => _wrappedCollection.ContainsKey(key) && _keyFilter(key);

		public bool TryGetValue(K key, [MaybeNullWhen(false)] out V value) => _wrappedCollection.TryGetValue(key, out value) && _keyFilter(key) && _valueFilter(value);
	}

	#endregion
}
