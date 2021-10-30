using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;

namespace CustomResources.Utils.Concepts.DataStructures
{
	public enum MemoryScope
	{
		Global,
		AsyncLocal,
		ThreadLocal
	}

	public interface IScopedCollection : ICollection
	{
		MemoryScope Scope { get; }
	}

	public interface IScopedCollectionWrapper<T, CollectionT> : IReadOnlyCollectionWrapper<T, CollectionT>, IScopedCollection
		where CollectionT : IReadOnlyCollection<T>, IScopedCollection
	{
		MemoryScope IScopedCollection.Scope => WrappedCollection.Scope;
	}

	public interface IScopedDictionary<K, V> : IScopedCollection, IConcurrentDictionary<K, V>, IElementContainer<K> { }


	public class ScopedConcurrentDictionary<K, V> : CustomDictionaryBase<K, V>, IScopedCollection, IInternalDictionary<K, V>, IScopedDictionary<K, V>,
		IScopedCollectionWrapper<KeyValuePair<K, IScopedBucket<V>>, WrappedScopeConcurrentDictionary<K, V>>, ICollectionWrapper<KeyValuePair<K, IScopedBucket<V>>, WrappedScopeConcurrentDictionary<K, V>>
	{
		protected readonly WrappedScopeConcurrentDictionary<K, V> _wrappedDictionary;
		private readonly Func<IScopedBucket<V>> _initializationFunc;
		public ScopedConcurrentDictionary(MemoryScope scope, IEqualityComparer<K> equalityComparer = null) : base(equalityComparer)
		{
			_wrappedDictionary = new WrappedScopeConcurrentDictionary<K, V>(scope);
			_initializationFunc = scope switch
			{
				MemoryScope.Global => () => new GlobalScopeBucket<V>(),
				MemoryScope.AsyncLocal => () => new AsyncLocalBucket<V>(),
				MemoryScope.ThreadLocal => () => new ThreadLocalBucket<V>(),
				_ => throw new NotImplementedException($"The given memory scope, {scope}, has yet to have an implementation added")
			};
		}

		private int _size = 0;

		public WrappedScopeConcurrentDictionary<K, V> WrappedCollection => _wrappedDictionary;

		public override bool IsSynchronized => _wrappedDictionary.As<ICollection>().IsSynchronized;

		public override object SyncRoot => _wrappedDictionary.As<ICollection>().SyncRoot;

		public override int Count => _size;
		public ISet<K> Keys => new KeyCollectionView<K, V, IReadOnlyDictionaryCollection<K, V>>(GetSnapshot(), EqualityComparer);
		public ICollection<V> Values => new ValueCollectionView<K, V, IReadOnlyDictionaryCollection<K, V>>(GetSnapshot());

		ICollection<K> IDictionary<K, V>.Keys => Keys;
		IEnumerable<K> IReadOnlyDictionary<K, V>.Keys => Keys;
		ICollection<V> IDictionary<K, V>.Values => Values;
		IEnumerable<V> IReadOnlyDictionary<K, V>.Values => Values;

		public override void Add(K key, V value)
		{
			if (!TryAdd(key, value))
				throw new ArgumentException($"The key already exists in the dictionary: {key}");
		}

		public override void Clear()
		{
			using var lockToken = _wrappedDictionary.FullLockToken();
			var buckets = _wrappedDictionary.Values;
			var bucketsAcquired = new List<IScopedBucket<V>>(buckets.Count);
			try
			{
				foreach (var bucket in buckets)
				{
					bucket.EnterLock();
					bucketsAcquired.Add(bucket);
				}
				lockToken.Dispose();
				foreach (var bucket in buckets)
					bucket.TryRemove(out _);
				_size = 0;
			}
			finally
			{
				foreach (var bucket in bucketsAcquired)
					bucket.ExitLock();
			}
		}

		public override IEnumerator<KeyValuePair<K, V>> GetEnumerator() => GetPairs(_wrappedDictionary).GetEnumerator();

		IReadOnlyDictionary<K, V> IConcurrentDictionary<K, V>.GetSnapshot() => GetSnapshot();
		public ReadOnlyDictionaryWrapper<K, V, K, IScopedBucket<V>, IReadOnlyDictionaryCollection<K, IScopedBucket<V>>> GetSnapshot() =>
			new ReadOnlyDictionaryWrapper<K, V, K, IScopedBucket<V>, IReadOnlyDictionaryCollection<K, IScopedBucket<V>>>(
				new ReadOnlyDictionaryFilter<K, IScopedBucket<V>, ReadOnlyDictionary<K, IScopedBucket<V>>>(
					_wrappedDictionary.GetSnapshot(),
					valueFilter: bucket => bucket.HasValue,
					keyEqualityComparer: EqualityComparer),
				Bijections<K>.Identity,
				bucket => bucket.Value,
				EqualityComparer);

		public override bool Remove(K key) => TryRemove(key, out _);

		public override void Update(K key, V value) => AddOrUpdate(key,
			k => Exceptions.Throw<V>(new KeyNotFoundException($"Cannot update a value in the dictionary when the key is not already in the dictionary: {key}")),
			(_, _) => value);


		public V AddOrUpdate(K key, V addValue, Func<K, V, V> updateValueFactory) => AddOrUpdate(key, _ => addValue, updateValueFactory);

		public override V AddOrUpdate(K key, Func<K, V> addValueFactory, Func<K, V, V> updateValueFactory) {
			Ensure.ArgumentNotNull(addValueFactory, nameof(addValueFactory));
			Ensure.ArgumentNotNull(updateValueFactory, nameof(updateValueFactory));
			return AddOrUpdate<object>(key,
				(k, _) => addValueFactory(k),
				(k, v, _) => updateValueFactory(k, v),
				default);
		}

		public V AddOrUpdate<A>(K key, Func<K, A, V> addValueFactory, Func<K, V, A, V> updateValueFactory, A factoryArgument)
		{
			var bucket = _wrappedDictionary.GetOrAdd(key, k => InstantiateBucket());
			while(true)
			{
				if (bucket.TryGetValue(out var currentValue))
				{
					var valueToSet = updateValueFactory(key, currentValue, factoryArgument);
					if (bucket.TrySet(valueToSet, currentValue))
						return valueToSet;
				}
				else
				{
					if (bucket.TryAdd(addValueFactory(key, factoryArgument), out var resultingValue))
					{
						Interlocked.Increment(ref _size);
						return resultingValue;
					}
				}
			}
		}

		public V GetOrAdd(K key, V value) => GetOrAdd(key, _ => value);
		public V GetOrAdd(K key, Func<K, V> valueFactory)
		{
			Ensure.ArgumentNotNull(valueFactory, nameof(valueFactory));
			return GetOrAdd<object>(key, (k, _) => valueFactory(k), default);
		}

		public V GetOrAdd<A>(K key, Func<K, A, V> valueFactory, A factoryArgument)
		{
			var bucket = _wrappedDictionary.GetOrAdd(key, k => InstantiateBucket());
			if (!bucket.TryGetValue(out var existingValue))
			{
				var newValue = valueFactory(key, factoryArgument);
				if (bucket.TryAdd(newValue, out existingValue))
					Interlocked.Increment(ref _size);
			}
			return existingValue;
		}

		public bool TryAdd(K key, V value)
		{
			var bucket = _wrappedDictionary.GetOrAdd(key, k => InstantiateBucket());
			var wasAdded = bucket.TryAdd(value, out _);
			if (wasAdded)
				Interlocked.Increment(ref _size);
			return wasAdded;
		}

		public override bool TryGetValue(K key, [MaybeNullWhen(false)] out V value)
		{
			if (!_wrappedDictionary.TryGetValue(key, out var bucket))
			{
				value = default;
				return false;
			}
			return bucket.TryGetValue(out value);
		}

		public bool TryRemove(K key, out V value)
		{
			if (_wrappedDictionary.TryGetValue(key, out var bucket) && bucket.TryRemove(out value))
			{
				Interlocked.Decrement(ref _size);
				return true;
			}
			value = default;
			return false;
		}

		public bool TryRemove(KeyValuePair<K, V> item)
		{
			var wasRemoved = _wrappedDictionary.TryGetValue(item.Key, out var bucket) && bucket.TrySet(default, item.Value, valueMeansRemoval: true);
			if (wasRemoved)
				Interlocked.Decrement(ref _size);
			return wasRemoved;
		}

		public bool TryUpdate(K key, V newValue, V comparisonValue) => _wrappedDictionary.TryGetValue(key, out var bucket) && bucket.TrySet(newValue, comparisonValue);

		private IScopedBucket<V> InstantiateBucket() => _initializationFunc();

		private static IEnumerable<KeyValuePair<K, V>> GetPairs(IReadOnlyDictionary<K, IScopedBucket<V>> dict)
		{
			foreach (var keyAndBucket in dict)
			{
				var bucket = keyAndBucket.Value;
				if (bucket.TryGetValue(out var bucketValue))
					yield return new KeyValuePair<K, V>(keyAndBucket.Key, bucketValue);
			}
		}
	}

	public class WrappedScopeConcurrentDictionary<K, V> : InternalConcurrentDictionary<K, IScopedBucket<V>>, IScopedCollection
	{
		public WrappedScopeConcurrentDictionary(MemoryScope scope)
		{
			Scope = scope;
		}

		public MemoryScope Scope { get; }
	}

	public interface IScopedBucket<V>
	{
		bool TryAdd(V value, out V resultingValue);
		bool TrySet(V newValue, V comparisonValue, bool valueMeansRemoval = false);
		bool TryRemove(out V oldValue);
		bool TryGetValue(out V value);

		internal void EnterLock();
		internal void ExitLock();

		public bool HasValue => TryGetValue(out _);
		public V Value => TryGetValue(out var value) ? value : Exceptions.Throw<V>(new InvalidOperationException("There is no value to retrieve"));
	}

	internal class GlobalScopeBucket<V> : IScopedBucket<V>
	{
		private readonly object _swapLock = new object();

		private (bool hasValue, V value) _value = (false, default);

		private V Value
		{
			set => _value = (true, value);
		}

		private void Delete()
		{
			_value = (false, default);
		}

		public bool TryAdd(V value, out V resultingValue)
		{
			if (TryGetValue(out resultingValue))
				return false;
			lock (_swapLock)
			{
				if (TryGetValue(out resultingValue))
					return false;
				Value = resultingValue = value;
				return true;
			}
		}

		public bool TryGetValue(out V value)
		{
			var (hasValue, currValue) = _value;
			value = currValue;
			return hasValue;
		}

		public bool TryRemove(out V oldValue)
		{
			if (!TryGetValue(out oldValue))
				return false;
			lock (_swapLock)
			{
				if (!TryGetValue(out oldValue))
					return false;
				Delete();
				return true;
			}
		}

		public bool TrySet(V newValue, V comparisonValue, bool valueMeansRemoval = false)
		{
			bool ShouldNotSwap() => !TryGetValue(out var snapshotValue) || !Equals(comparisonValue, snapshotValue);
			if (ShouldNotSwap())
				return false;
			lock(_swapLock)
			{
				if (ShouldNotSwap())
					return false;
				if (valueMeansRemoval)
					Delete();
				else
					Value = newValue;
				return true;
			}
		}

		void IScopedBucket<V>.EnterLock()
		{
			Monitor.Enter(_swapLock);
		}

		void IScopedBucket<V>.ExitLock()
		{
			Monitor.Exit(_swapLock);
		}
	}



	internal abstract class LocalBucket<V> : IScopedBucket<V>
	{
		protected abstract (bool hasValue, V value) Value { get; set; }

		public bool TryAdd(V value, out V resultingValue)
		{
			if (TryGetValue(out resultingValue))
				return false;
			Value = (true, value);
			resultingValue = value;
			return true;
		}

		public bool TryGetValue(out V value)
		{
			var (hasValue, currValue) = Value;
			value = currValue;
			return hasValue;
		}

		public bool TryRemove(out V oldValue)
		{
			if (!TryGetValue(out oldValue))
				return false;
			Value = (false, default);
			return true;
		}

		public bool TrySet(V newValue, V comparisonValue, bool valueMeansRemoval = false)
		{
			if (!TryGetValue(out var oldValue) || !Equals(comparisonValue, oldValue))
				return false;
			if (valueMeansRemoval)
				Value = (false, default);
			else
				Value = (true, newValue);
			return true;
		}
		void IScopedBucket<V>.EnterLock() { /* Nothing to do */ }

		void IScopedBucket<V>.ExitLock() { /* Nothing to do */ }
	}

	internal class ThreadLocalBucket<V> : LocalBucket<V>
	{
		private readonly ThreadLocal<(bool hasValue, V value)> _store = new ThreadLocal<(bool hasValue, V value)>(() => (false, default));
		protected override (bool hasValue, V value) Value { get => _store.Value; set => _store.Value = value; }
	}

	internal class AsyncLocalBucket<V> : LocalBucket<V>
	{
		private readonly AsyncLocal<(bool hasValue, V value)> _store= new AsyncLocal<(bool hasValue, V value)>{ Value = (false, default) };
		protected override (bool hasValue, V value) Value { get => _store.Value; set => _store.Value = value; }
	}
}
