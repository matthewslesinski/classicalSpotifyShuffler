using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;

namespace CustomResources.Utils.Concepts.DataStructures
{
	public enum OverridesScope
	{
		Global,
		AsyncLocal,
		ThreadLocal
	}

	public interface IOverriddenDictionary<K, V> : IReadOnlyDictionary<K, V>
	{
		public IDisposable AddOverride(K key, V value);
	}

	public class OverridesDictionary<K, V> : CustomDictionaryBase<K, V>, IOverriddenDictionary<K, V>
	{
		private readonly Dictionary<K, IOverridesBucket> _overrides;
		private readonly IDictionary<K, V> _wrappedDictionary;
		private readonly OverridesScope _overridesScope;

		public OverridesDictionary(Dictionary<K, V> wrappedDictionary, OverridesScope overridesScope = OverridesScope.AsyncLocal) : this(wrappedDictionary, overridesScope, wrappedDictionary.Comparer) { }
		public OverridesDictionary(IDictionary<K, V> wrappedDictionary, OverridesScope overridesScope = OverridesScope.AsyncLocal, IEqualityComparer<K> equalityComparer = null) : base(equalityComparer ?? EqualityComparer<K>.Default)
		{
			Ensure.ArgumentNotNull(wrappedDictionary, nameof(wrappedDictionary));

			_wrappedDictionary = wrappedDictionary;
			_overridesScope = overridesScope;
			_overrides = new Dictionary<K, IOverridesBucket>(_equalityComparer);
		}

		public override void Add(K key, V value) => _wrappedDictionary.Add(key, value);

		public override void Clear() => _wrappedDictionary.Clear();

		public override IEnumerator<KeyValuePair<K, V>> GetEnumerator() => _wrappedDictionary.Keys.Concat(_overrides.Keys).Distinct(_equalityComparer).Select(key => new KeyValuePair<K, V>(key, this.Get(key))).GetEnumerator();

		public override bool Remove(K key) => _wrappedDictionary.Remove(key);

		public override bool TryGetValue(K key, [MaybeNullWhen(false)] out V value) => (_overrides.TryGetValue(key, out var bucket) && bucket.TryPeek(out value)) || _wrappedDictionary.TryGetValue(key, out value);

		public override void Update(K key, V value) => _wrappedDictionary[key] = value;

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
				OverridesScope.Global => new GlobalBucket(),
				OverridesScope.AsyncLocal => new AsyncLocalBucket(),
				OverridesScope.ThreadLocal => new ThreadLocalBucket(),
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
			private ConcurrentStack<V> _stack = new();

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
