using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;

namespace CustomResources.Utils.Concepts.DataStructures
{
	public interface IGenericInternalReadOnlyCollection<T> : IReadOnlyCollection<T>
	{
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		protected static void CopyToImpl(Array array, int index, IReadOnlyCollection<T> elements)
		{
			Ensure.ArgumentNotNull(array, nameof(array));
			if (index < 0)
				throw new ArgumentOutOfRangeException(nameof(index));
			var space = array.Length - index;
			if (elements.Count > space)
				throw new ArgumentException("Not enough space in destination array to fit the elements");
			foreach (var element in elements)
				array.SetValue(element, index++);
		}
	}

	public interface IGenericInternalCollection<T> : ICollection<T>, IGenericInternalReadOnlyCollection<T>
	{
		void ICollection<T>.CopyTo(T[] array, int index) => CopyToImpl(array, index, this);
	}

	public interface IInternalReadOnlyCollection<T> : IGenericInternalReadOnlyCollection<T>, ICollection
	{
		void ICollection.CopyTo(Array array, int index) => CopyToImpl(array, index, this);
	}

	public interface IInternalCollection<T> : IInternalReadOnlyCollection<T>, IGenericInternalCollection<T> { }

	public interface IReadOnlyDictionaryCollection<K, V> : IReadOnlyDictionary<K, V>, IInternalReadOnlyCollection<KeyValuePair<K, V>>, IElementContainer<K> { }

	public interface IDictionaryCollection<K, V> : IReadOnlyDictionaryCollection<K, V>, IInternalCollection<KeyValuePair<K, V>>, IDictionary<K, V> { }

	public interface IElementContainer<T>
	{
		IEqualityComparer<T> EqualityComparer { get; }
	}

	public interface IMappedElementContainer<S, T> : IElementContainer<S>
	{
		IEqualityComparer<T> WrappedEqualityComparer { get; }
		Func<S, T> ElementMapper { get; }
		IEqualityComparer<S> IElementContainer<S>.EqualityComparer => new KeyBasedEqualityComparer<S, T>(ElementMapper, WrappedEqualityComparer);
	}

	public interface IConcurrentCollection<T, out ReadOnlyViewT> : IInternalCollection<T> where ReadOnlyViewT : IReadOnlyCollection<T> 
	{
		ReadOnlyViewT GetSnapshot();

		void ICollection<T>.CopyTo(T[] array, int index) => CopyToImpl(array, index, GetSnapshot());
		void ICollection.CopyTo(Array array, int index) => CopyToImpl(array, index, GetSnapshot());
	}

	#region Set Interfaces

	public interface IInternalSet<T> : ISet<T>, IReadOnlySet<T>, IInternalCollection<T>, IElementContainer<T>
	{
		void ICollection<T>.Add(T item)
		{
			if (IsReadOnly)
				throw new NotSupportedException("Cannot add elements to a readonly set");
			Add(item);
		}

		void ISet<T>.ExceptWith(IEnumerable<T> other) => ExceptWithNaive(other);
		protected sealed void ExceptWithNaive(IEnumerable<T> other)
		{
			Ensure.ArgumentNotNull(other, nameof(other));
			foreach (var o in other)
				Remove(o);
		}

		void ISet<T>.IntersectWith(IEnumerable<T> other) => IntersectWithNaive(other);
		protected sealed void IntersectWithNaive(IEnumerable<T> other)
		{
			Ensure.ArgumentNotNull(other, nameof(other));
			if (other is not IReadOnlySet<T> otherSet)
				otherSet = other.ToHashSet();
			var toRemoves = this.Where(otherSet.NotContains).ToList();
			foreach (var toRemove in toRemoves)
				Remove(toRemove);
		}

		bool IReadOnlySet<T>.IsProperSubsetOf(IEnumerable<T> other) => this.As<ISet<T>>().IsProperSubsetOf(other);

		bool ISet<T>.IsProperSubsetOf(IEnumerable<T> other) =>
			(other is IReadOnlySet<T> otherSet ? otherSet.Count : other.EnsureNotNull(nameof(other)).Distinct(EqualityComparer).Count()) > this.As<IReadOnlySet<T>>().Count
				&& this.As<ISet<T>>().IsSubsetOf(other);

		bool IReadOnlySet<T>.IsProperSupersetOf(IEnumerable<T> other) => this.As<ISet<T>>().IsProperSupersetOf(other);

		bool ISet<T>.IsProperSupersetOf(IEnumerable<T> other) =>
			(other is IReadOnlySet<T> otherSet ? otherSet.Count : other.EnsureNotNull(nameof(other)).Distinct(EqualityComparer).Count()) < this.As<IReadOnlySet<T>>().Count
				&& this.As<IReadOnlySet<T>>().IsSupersetOf(other);

		bool IReadOnlySet<T>.IsSubsetOf(IEnumerable<T> other) => this.As<ISet<T>>().IsSubsetOf(other);

		bool ISet<T>.IsSubsetOf(IEnumerable<T> other) => IsSubsetOfNaive(other);
		protected sealed bool IsSubsetOfNaive(IEnumerable<T> other)
		{
			Ensure.ArgumentNotNull(other, nameof(other));
			var count = this.As<IReadOnlySet<T>>().Count;
			return !(other is IReadOnlySet<T> otherSet && otherSet.Count < count) && other.Where(this.As<IReadOnlySet<T>>().Contains).Distinct(EqualityComparer).Count() == count;
		}

		bool IReadOnlySet<T>.IsSupersetOf(IEnumerable<T> other) => this.As<ISet<T>>().IsSupersetOf(other);

		bool ISet<T>.IsSupersetOf(IEnumerable<T> other) => IsSupersetOfNaive(other);
		protected sealed bool IsSupersetOfNaive(IEnumerable<T> other)
		{
			Ensure.ArgumentNotNull(other, nameof(other));
			var count = this.As<ICollection<T>>().Count;
			return !(other is IReadOnlySet<T> otherSet && otherSet.Count > count) && other.All(this.As<IReadOnlySet<T>>().Contains);
		}

		bool IReadOnlySet<T>.Overlaps(IEnumerable<T> other) => this.As<ISet<T>>().Overlaps(other);

		bool ISet<T>.Overlaps(IEnumerable<T> other) => OverlapsNaive(other);
		protected sealed bool OverlapsNaive(IEnumerable<T> other)
		{
			Ensure.ArgumentNotNull(other, nameof(other));
			return other.Any(this.As<IReadOnlySet<T>>().Contains);
		}

		bool IReadOnlySet<T>.SetEquals(IEnumerable<T> other) => this.As<ISet<T>>().SetEquals(other);

		bool ISet<T>.SetEquals(IEnumerable<T> other) => SetEqualsNaive(other);
		protected sealed bool SetEqualsNaive(IEnumerable<T> other)
		{
			Ensure.ArgumentNotNull(other, nameof(other));
			if (other is not IReadOnlySet<T> otherSet)
				otherSet = other.ToHashSet(EqualityComparer);
			var count = this.As<IReadOnlySet<T>>().Count;
			return otherSet.Count == count && this.Intersect(otherSet).Count() == count;
		}

		void ISet<T>.SymmetricExceptWith(IEnumerable<T> other) => SymmetricExceptWithNaive(other);
		protected sealed void SymmetricExceptWithNaive(IEnumerable<T> other)
		{
			Ensure.ArgumentNotNull(other, nameof(other));
			foreach (var o in other.Distinct(EqualityComparer))
			{
				if (this.As<IReadOnlySet<T>>().Contains(o))
					Remove(o);
				else
					Add(o);
			}
		}

		void ISet<T>.UnionWith(IEnumerable<T> other) => UnionWithNaive(other);
		protected sealed void UnionWithNaive(IEnumerable<T> other)
		{
			Ensure.ArgumentNotNull(other, nameof(other));
			foreach (var o in other)
				Add(o);
		}
	}


	public interface IInternalSet<T, in SetT> : IInternalSet<T> where SetT : IInternalSet<T, SetT>, IReadOnlySet<T>
	{
		void ISet<T>.ExceptWith(IEnumerable<T> other)
		{
			if (other is SetT otherSet && ExceptWith(otherSet))
				return;
			ExceptWithNaive(other);			
		}

		bool ExceptWith(SetT otherSet);

		void ISet<T>.IntersectWith(IEnumerable<T> other)
		{
			if (other is SetT otherSet && IntersectWith(otherSet))
				return;
			IntersectWithNaive(other);
		}

		bool IntersectWith(SetT otherSet);

		bool ISet<T>.IsSubsetOf(IEnumerable<T> other) => other is SetT otherSetT ? IsSubsetOf(otherSetT) : IsSubsetOfNaive(other);

		bool IsSubsetOf(SetT otherSet);

		bool ISet<T>.IsSupersetOf(IEnumerable<T> other) => other is SetT otherSetT ? IsSupersetOf(otherSetT) : IsSupersetOfNaive(other);

		bool IsSupersetOf(SetT otherSet);

		bool ISet<T>.Overlaps(IEnumerable<T> other) => other is SetT otherSetT ? Overlaps(otherSetT) : OverlapsNaive(other);

		bool Overlaps(SetT otherSet);

		bool ISet<T>.SetEquals(IEnumerable<T> other) => other is SetT otherSetT ? SetEquals(otherSetT) : SetEqualsNaive(other);

		bool SetEquals(SetT otherSet);

		void ISet<T>.SymmetricExceptWith(IEnumerable<T> other)
		{
			if (other is SetT otherSet && SymmetricExceptWith(otherSet))
				return;
			SymmetricExceptWithNaive(other);
		}

		bool SymmetricExceptWith(SetT otherSet);

		void ISet<T>.UnionWith(IEnumerable<T> other)
		{
			if (other is SetT otherSet && UnionWith(otherSet))
				return;
			UnionWithNaive(other);
		}

		bool UnionWith(SetT otherSet);
	}

	#endregion

	public interface IInternalReadOnlyDictionary<K, V> : IReadOnlyDictionary<K, V>, IReadOnlyDictionaryCollection<K, V>, IElementContainer<K>
	{
		IEnumerable<K> IReadOnlyDictionary<K, V>.Keys => this.Select(kvp => kvp.Key);
		IEnumerable<V> IReadOnlyDictionary<K, V>.Values => this.Select(kvp => kvp.Value);

		bool IReadOnlyDictionary<K, V>.ContainsKey(K key) => this.As<IReadOnlyDictionary<K, V>>().TryGetValue(key, out _);
	}

	public interface IInternalDictionary<K, V> : IDictionaryCollection<K, V>, IInternalReadOnlyDictionary<K, V>, IInternalReadOnlyCollection<KeyValuePair<K, V>>,
		IGenericInternalCollection<KeyValuePair<K, V>>, IElementContainer<K>
	{
		public new KeyCollectionView<K, V, IInternalDictionary<K, V>> Keys => new KeyCollectionView<K, V, IInternalDictionary<K, V>>(this, EqualityComparer);
		public new ValueCollectionView<K, V, IInternalDictionary<K, V>> Values => new ValueCollectionView<K, V, IInternalDictionary<K, V>>(this);
		ICollection<K> IDictionary<K, V>.Keys => Keys;
		ICollection<V> IDictionary<K, V>.Values => Values;
		IEnumerable<K> IReadOnlyDictionary<K, V>.Keys => Keys;
		IEnumerable<V> IReadOnlyDictionary<K, V>.Values => Values;

		void ICollection<KeyValuePair<K, V>>.Add(KeyValuePair<K, V> item) => Add(item.Key, item.Value);
		bool ICollection<KeyValuePair<K, V>>.Remove(KeyValuePair<K, V> item) => this.As<IDictionary<K, V>>().TryGetValue(item.Key, out var existingValue)
			&& Equals(item.Value, existingValue) && Remove(item.Key);

		bool ICollection<KeyValuePair<K, V>>.Contains(KeyValuePair<K, V> item) => this.As<IReadOnlyDictionary<K, V>>().TryGetValue(item.Key, out var foundValue) && Equals(foundValue, item.Value);
		bool IDictionary<K, V>.ContainsKey(K key) => this.As<IDictionary<K, V>>().TryGetValue(key, out _);
		public bool ContainsValue(V value, IEqualityComparer<V> equalityComparer = null)
		{
			var values = Values;
			return equalityComparer == null ? values.Contains(value) : values.ContainsValue(value, equalityComparer);
		}
	}
}
