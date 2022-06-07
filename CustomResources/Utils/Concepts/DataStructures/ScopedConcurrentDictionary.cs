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

	public interface IScopedCollectionWrapper<T, CollectionT> : IWrapper<CollectionT>, IScopedCollection
		where CollectionT : IReadOnlyCollection<T>, IScopedCollection
	{
		MemoryScope IScopedCollection.Scope => WrappedObject.Scope;
	}

	public interface IScopedDictionary<K, V> : IScopedCollection, IConcurrentDictionary<K, V>, IElementContainer<K> { }


	public class ScopedConcurrentDictionary<K, V> : CustomDictionaryBase<K, V>, IScopedCollection, IInternalDictionary<K, V>, IScopedDictionary<K, V>,
		IConcurrentDictionary<K, V>, IScopedCollectionWrapper<KeyValuePair<K, IScopedBucket<V>>, WrappedScopeConcurrentDictionary<K, V>>,
		ICollectionWrapper<KeyValuePair<K, IScopedBucket<V>>, WrappedScopeConcurrentDictionary<K, V>>
	{
		protected readonly WrappedScopeConcurrentDictionary<K, V> _wrappedDictionary;
		private readonly Func<IScopedBucket<V>> _initializationFunc;
		public ScopedConcurrentDictionary(MemoryScope scope, IEqualityComparer<K> equalityComparer = null) : base(equalityComparer)
		{
			_wrappedDictionary = new WrappedScopeConcurrentDictionary<K, V>(scope);
			_sizeHolder = scope switch
			{
				MemoryScope.Global => new GlobalScopeIntBucket(),
				MemoryScope.AsyncLocal => new AsyncLocalIntBucket(),
				MemoryScope.ThreadLocal => new ThreadLocalIntBucket(),
				_ => throw new NotImplementedException($"The given memory scope, {scope}, has yet to have an implementation added")
			};
			_initializationFunc = scope switch
			{
				MemoryScope.Global => () => new GlobalScopeBucket<V>(_sizeHolder),
				MemoryScope.AsyncLocal => () => new AsyncLocalBucket<V>(_sizeHolder),
				MemoryScope.ThreadLocal => () => new ThreadLocalBucket<V>(_sizeHolder),
				_ => throw new NotImplementedException($"The given memory scope, {scope}, has yet to have an implementation added")
			};
		}

		private readonly IScopedIntBucket _sizeHolder;

		public WrappedScopeConcurrentDictionary<K, V> WrappedObject => _wrappedDictionary;

		public override bool IsSynchronized => _wrappedDictionary.As<ICollection>().IsSynchronized;

		public override object SyncRoot => _wrappedDictionary.As<ICollection>().SyncRoot;

		public override int Count => _sizeHolder.Value;
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
			}
			finally
			{
				foreach (var bucket in bucketsAcquired)
					bucket.ExitLock();
			}
		}

		public override IEnumerator<KeyValuePair<K, V>> GetEnumerator() => GetPairs(_wrappedDictionary).GetEnumerator();

		IReadOnlyDictionary<K, V> IConcurrentCollection<KeyValuePair<K, V>, IReadOnlyDictionary<K, V>>.GetSnapshot() => GetSnapshot();
		public ReadOnlyDictionaryWrapper<K, V, K, IScopedBucket<V>, IReadOnlyDictionaryCollection<K, IScopedBucket<V>>> GetSnapshot() =>
			_wrappedDictionary.GetSnapshot()
				.WhereAsDictionary<K, IScopedBucket<V>, ReadOnlyDictionary<K, IScopedBucket<V>>>(valueFilter: bucket => bucket.HasValue, keyEqualityComparer: EqualityComparer)
				.SelectAsDictionary<K, V, K, IScopedBucket<V>, IReadOnlyDictionaryCollection<K, IScopedBucket<V>>>(
					Bijections<K>.Identity,
					bucket => bucket.Value,
					EqualityComparer);

		public override bool Remove(K key) => TryRemove(key, out _);

		public override void Update(K key, V value) => AddOrUpdate(
			key,
			k => Exceptions.Throw<V>(new KeyNotFoundException($"Cannot update a value in the dictionary when the key is not already in the dictionary: {key}")),
			(_, _) => value);


		public V AddOrUpdate(K key, V addValue, Func<K, V, V> updateValueFactory) => AddOrUpdate(key, _ => addValue, updateValueFactory);

		public override V AddOrUpdate(K key, Func<K, V> addValueFactory, Func<K, V, V> updateValueFactory) {
			Ensure.ArgumentNotNull(addValueFactory, nameof(addValueFactory));
			Ensure.ArgumentNotNull(updateValueFactory, nameof(updateValueFactory));
			// Use generic typing of object because the type is only used for arguments that don't matter
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
						return resultingValue;
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
				bucket.TryAdd(newValue, out existingValue);
			}
			return existingValue;
		}

		public bool TryAdd(K key, V value)
		{
			var bucket = _wrappedDictionary.GetOrAdd(key, k => InstantiateBucket());
			return bucket.TryAdd(value, out _);
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
				return true;
			value = default;
			return false;
		}

		public bool TryRemove(KeyValuePair<K, V> item) => _wrappedDictionary.TryGetValue(item.Key, out var bucket) && bucket.TrySet(default, item.Value, valueMeansRemoval: true);

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

	public interface IScopedIntBucket
	{
		public int Increment();
		public int Decrement();
		public void Reset();
		public int Value { get; }
	}

	internal class GlobalScopeBucket<V> : IScopedBucket<V>
	{
		private readonly object _swapLock = new object();
		private readonly IScopedIntBucket _dictSizeCounter;

		internal GlobalScopeBucket(IScopedIntBucket sizeCounter)
		{
			_dictSizeCounter = sizeCounter;
		}

		protected Reference<V> _value = null;

		private V Value
		{
			set => _value = new(value);
		}

		private void Delete()
		{
			_value = null;
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
				_dictSizeCounter.Increment();
				return true;
			}
		}

		public bool TryGetValue(out V value) => _value.TryGetValue(out value);

		public bool TryRemove(out V oldValue)
		{
			if (!TryGetValue(out oldValue))
				return false;
			lock (_swapLock)
			{
				if (!TryGetValue(out oldValue))
					return false;
				Delete();
				_dictSizeCounter.Decrement();
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
				{
					Delete();
					_dictSizeCounter.Decrement();
				}
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

	internal class GlobalScopeIntBucket : IScopedIntBucket
	{
		private int _value = 0;
		public int Value { get => _value; private set => _value = value; }
		public int Decrement() => Interlocked.Decrement(ref _value);
		public int Increment() => Interlocked.Increment(ref _value);
		public void Reset() => _value = 0;
	}

	internal abstract class LocalBucket<V> : IScopedBucket<V>
	{
		private readonly IScopedIntBucket _dictSizeCounter;

		internal LocalBucket(IScopedIntBucket sizeCounter)
		{
			_dictSizeCounter = sizeCounter;
		}

		protected abstract Reference<V> Value { get; set; }

		public bool TryAdd(V value, out V resultingValue)
		{
			if (TryGetValue(out resultingValue))
				return false;
			Value = new(value);
			resultingValue = value;
			_dictSizeCounter.Increment();
			return true;
		}

		public bool TryGetValue(out V value) => Value.TryGetValue(out value);

		public bool TryRemove(out V oldValue)
		{
			if (!TryGetValue(out oldValue))
				return false;
			Value = null;
			_dictSizeCounter.Decrement();
			return true;
		}

		public bool TrySet(V newValue, V comparisonValue, bool valueMeansRemoval = false)
		{
			if (!TryGetValue(out var oldValue) || !Equals(comparisonValue, oldValue))
				return false;
			if (valueMeansRemoval)
			{
				Value = null;
				_dictSizeCounter.Decrement();
			}
			else
				Value = new(newValue);
			return true;
		}
		void IScopedBucket<V>.EnterLock() { /* Nothing to do */ }

		void IScopedBucket<V>.ExitLock() { /* Nothing to do */ }
	}

	internal class ThreadLocalBucket<V> : LocalBucket<V>
	{
		internal ThreadLocalBucket(IScopedIntBucket sizeCounter) : base(sizeCounter) { }

		private readonly ThreadLocal<Reference<V>> _store = new ThreadLocal<Reference<V>>(() => null);
		protected override Reference<V> Value { get => _store.Value; set => _store.Value = value; }
	}

	internal class ThreadLocalIntBucket : IScopedIntBucket
	{
		private readonly ThreadLocal<int> _valueStore = new ThreadLocal<int>(() => 0);

		public int Value => _valueStore.Value;
		public int Decrement() => _valueStore.Value--;
		public int Increment() => _valueStore.Value++;
		public void Reset() => _valueStore.Value = 0;
	}

	internal class AsyncLocalBucket<V> : LocalBucket<V>
	{
		internal AsyncLocalBucket(IScopedIntBucket sizeCounter) : base(sizeCounter) { }

		private readonly AsyncLocal<Reference<V>> _store= new AsyncLocal<Reference<V>>{ Value = null };
		protected override Reference<V> Value { get => _store.Value; set => _store.Value = value; }
	}

	internal class AsyncLocalIntBucket : IScopedIntBucket
	{
		private readonly AsyncLocal<int> _valueStore = new AsyncLocal<int>();

		public int Value => _valueStore.Value;
		public int Decrement() => _valueStore.Value--;
		public int Increment() => _valueStore.Value++;
		public void Reset() => _valueStore.Value = 0;
	}
}
