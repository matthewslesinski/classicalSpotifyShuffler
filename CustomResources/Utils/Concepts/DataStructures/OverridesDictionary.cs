using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;

namespace CustomResources.Utils.Concepts.DataStructures
{
	public interface IOverrideableDictionary<K, V>
	{
		IDisposable AddOverrides(params (K key, V value)[] keyValuePairs);
		IDisposable AddOverrides(IEnumerable<(K key, V value)> keyValuePairs);
		IDisposable AddOverride(K key, V value);

		public bool TryGetValue(K key, out V value);
	}

	public class OverridesDictionary<K, V> : CustomDictionaryBase<K, V>, IOverrideableDictionary<K, V>, IScopedCollection
	{
		public MemoryScope Scope => _overridesScope;
		public override bool IsSynchronized { get; }
		public override object SyncRoot { get; } = new object();

		private readonly IDictionary<K, IOverridesBucket> _overrides;
		private readonly IDictionary<K, V> _wrappedDictionary;
		private readonly MemoryScope _overridesScope;

		public OverridesDictionary(IScopedDictionary<K, V> wrappedDictionary) : this(wrappedDictionary, wrappedDictionary.Scope, wrappedDictionary.EqualityComparer) { }
		public OverridesDictionary(IConcurrentDictionary<K, V> wrappedDictionary, MemoryScope overridesScope = MemoryScope.AsyncLocal, IEqualityComparer<K> equalityComparer = null)
			: this(wrappedDictionary, true, overridesScope, equalityComparer) { }
		public OverridesDictionary(Dictionary<K, V> wrappedDictionary, bool shouldBeThreadSafe, MemoryScope overridesScope = MemoryScope.AsyncLocal)
			: this(wrappedDictionary, shouldBeThreadSafe, overridesScope, wrappedDictionary.Comparer) { }
		public OverridesDictionary(IInternalDictionary<K, V> wrappedDictionary, bool shouldBeThreadSafe, MemoryScope overridesScope = MemoryScope.AsyncLocal)
			: this(wrappedDictionary, shouldBeThreadSafe, overridesScope, wrappedDictionary.EqualityComparer) { }
		public OverridesDictionary(IDictionary<K, V> wrappedDictionary, bool shouldBeThreadSafe, MemoryScope overridesScope = MemoryScope.AsyncLocal, IEqualityComparer<K> equalityComparer = null)
			: base(equalityComparer ?? EqualityComparer<K>.Default)
		{
			Ensure.ArgumentNotNull(wrappedDictionary, nameof(wrappedDictionary));

			_wrappedDictionary = wrappedDictionary;
			_overridesScope = overridesScope;
			_overrides = shouldBeThreadSafe ? new InternalConcurrentDictionary<K, IOverridesBucket>(_equalityComparer) : new Dictionary<K, IOverridesBucket>(_equalityComparer);
			IsSynchronized = shouldBeThreadSafe;
		}

		public override int Count => _wrappedDictionary.Count;

		public override void Add(K key, V value) => _wrappedDictionary.Add(key, value);

		public override V AddOrUpdate(K key, Func<K, V> addValueFactory, Func<K, V, V> updateValueFactory)
		{
			V newValue;
			if (_wrappedDictionary.TryGetValue(key, out var existingValue))
				Update(key, newValue = updateValueFactory(key, existingValue));
			else
				Add(key, newValue = addValueFactory(key));
			return newValue;
		}

		public override void Clear() => _wrappedDictionary.Clear();

		public override IEnumerator<KeyValuePair<K, V>> GetEnumerator()
		{
			IEnumerable<KeyValuePair<K, V>> wrappedElements = _wrappedDictionary is IConcurrentDictionary<K, V> concurrentWrappedDict
				? concurrentWrappedDict.GetSnapshot()
				: _wrappedDictionary;
			IEnumerable<KeyValuePair<K, IOverridesBucket>> overrideBuckets = _overrides is IConcurrentDictionary<K, IOverridesBucket> concurrentOverrideDict
				? concurrentOverrideDict.GetSnapshot()
				: _overrides;
			var seenKeys = new HashSet<K>(EqualityComparer);
			foreach(var keyBucketPair in overrideBuckets)
			{
				if (keyBucketPair.Value.TryPeek(out var overrideValue))
				{
					seenKeys.Add(keyBucketPair.Key);
					yield return new KeyValuePair<K, V>(keyBucketPair.Key, overrideValue);
				}
			}
			foreach(var wrappedElement in wrappedElements)
			{
				if (!seenKeys.Contains(wrappedElement.Key))
					yield return wrappedElement;
			}
		}

		public override bool Remove(K key) => _wrappedDictionary.Remove(key);

		public override bool TryGetValue(K key, [MaybeNullWhen(false)] out V value) => (_overrides.TryGetValue(key, out var bucket) && bucket.TryPeek(out value)) || _wrappedDictionary.TryGetValue(key, out value);

		public override void Update(K key, V value) => _wrappedDictionary[key] = value;

		public IDisposable AddOverrides(params (K key, V value)[] keyValuePairs) => AddOverrides(keyValuePairs.As<IEnumerable<(K key, V value)>>());
		public IDisposable AddOverrides(IEnumerable<(K key, V value)> keyValuePairs) => new MultipleDisposables(keyValuePairs.Select(kvp => AddOverride(kvp.key, kvp.value)).ToArray());
		public IDisposable AddOverride(K key, V value)
		{
			var bucket = _overrides.AddIfNotPresent(key, InstantiateBucket);
			bucket.Push(value);
			void Pop()
			{
				var finishedValue = bucket.Pop();
				if (!Equals(finishedValue, value))
					throw new InvalidOperationException($"The override dictionary has been corrupted. Attemping to remove override {value} for key {key} but found value {finishedValue} instead");
			}
			return new DisposableAction(Pop);
		}

		private IOverridesBucket InstantiateBucket()
		{
			return _overridesScope switch
			{
				MemoryScope.Global => new GlobalBucket(),
				MemoryScope.AsyncLocal => new AsyncLocalBucket(),
				MemoryScope.ThreadLocal => new ThreadLocalBucket(),
				_ => throw new NotImplementedException("Handling for other scopes has not been implemented yet"),
			};
		}

		private interface IOverridesBucket
		{
			bool TryPeek(out V value);
			void Push(V value);
			V Pop();

			public bool HasValue() => TryPeek(out _);
			public V Peek() => TryPeek(out var value) ? value : Exceptions.Throw<V>(new InvalidOperationException("There was no value to get"));
		}

		private class GlobalBucket : IOverridesBucket
		{
			private readonly ConcurrentStack<V> _stack = new();

			public V Pop() => _stack.TryPop(out var value) ? value : Exceptions.Throw<V>(new InvalidOperationException("Cannot pop an empty bucket's stack"));
			public void Push(V value) => _stack.Push(value);
			public bool TryPeek(out V value) => _stack.TryPeek(out value);
		}

		private abstract class LocalStorageBucket<StorageMechanism> : IOverridesBucket where StorageMechanism : class, ILocalStorageWrapper<StorageMechanism>
		{
			private readonly StorageMechanism _top;

			protected LocalStorageBucket()
			{
				_top = CreateNewStore(default);
			}

			public V Pop()
			{
				if (!_top.TryGetNext(out var next))
					throw new InvalidOperationException("Cannot pop an empty bucket's stack");
				_top.Next = next.Next;
				return next.Value;
			}

			public void Push(V value)
			{
				var newNext = CreateNewStore(value);
				var currNext = _top.Next;
				newNext.Next = currNext;
				_top.Next = newNext;
			}

			public bool TryPeek(out V value)
			{
				var hasValue = _top.TryGetNext(out var next);
				value = next == null ? default : next.Value;
				return hasValue;
			}

			protected abstract StorageMechanism CreateNewStore(V value);
		}

		protected interface ILocalStorageWrapper<StorageMechanism> where StorageMechanism : class, ILocalStorageWrapper<StorageMechanism>
		{
			StorageMechanism Next { get; set; }
			V Value { get; }

			internal bool TryGetNext(out StorageMechanism next)
			{
				next = Next;
				return Next != null;
			}
		}

		private class AsyncLocalBucket : LocalStorageBucket<AsyncLocalWrapper>
		{
			protected override AsyncLocalWrapper CreateNewStore(V value) => new AsyncLocalWrapper { Value = value };
		}

		private class AsyncLocalWrapper : ILocalStorageWrapper<AsyncLocalWrapper>
		{
			private readonly AsyncLocal<AsyncLocalWrapper> _wrappedStore = new();

			public AsyncLocalWrapper Next
			{
				get => _wrappedStore.Value;
				set => _wrappedStore.Value = value;
			}

			public V Value { get; set; }
		}

		private class ThreadLocalBucket : LocalStorageBucket<ThreadLocalWrapper>
		{
			protected override ThreadLocalWrapper CreateNewStore(V value) => new ThreadLocalWrapper { Value = value };
		}

		private class ThreadLocalWrapper : ILocalStorageWrapper<ThreadLocalWrapper>
		{
			private readonly ThreadLocal<ThreadLocalWrapper> _wrappedStore = new();

			public ThreadLocalWrapper Next
			{
				get => _wrappedStore.Value;
				set => _wrappedStore.Value = value;
			}

			public V Value { get; set; }
		}
	}
}
