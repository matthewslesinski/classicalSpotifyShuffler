using System;
using System.Collections;
using System.Collections.Generic;

namespace CustomResources.Utils.Concepts.DataStructures
{
	public interface IConcurrentSet<T> : IConcurrentCollection<T, IReadOnlySet<T>>, IReadOnlySet<T>, IElementContainer<T>
	{
		new bool Add(T item);
		bool IReadOnlySet<T>.IsProperSubsetOf(IEnumerable<T> other) => GetSnapshot().IsProperSubsetOf(other);
		bool IReadOnlySet<T>.IsProperSupersetOf(IEnumerable<T> other) => GetSnapshot().IsProperSupersetOf(other);
		bool IReadOnlySet<T>.IsSubsetOf(IEnumerable<T> other) => GetSnapshot().IsSubsetOf(other);
		bool IReadOnlySet<T>.IsSupersetOf(IEnumerable<T> other) => GetSnapshot().IsSupersetOf(other);
		bool IReadOnlySet<T>.Overlaps(IEnumerable<T> other) => GetSnapshot().Overlaps(other);
		bool IReadOnlySet<T>.SetEquals(IEnumerable<T> other) => GetSnapshot().SetEquals(other);
	}

	public class InternalConcurrentSet<T> : CollectionWrapper<T, KeyValuePair<T, T>, InternalConcurrentDictionary<T, T>>, IConcurrentSet<T>,
		IWrappedElementContainer<T, InternalConcurrentDictionary<T, T>>
	{
		public InternalConcurrentSet(IEqualityComparer<T> equalityComparer = null)
			: base(new InternalConcurrentDictionary<T, T>(equalityComparer), new Bijection<T, KeyValuePair<T, T>>(t => new KeyValuePair<T, T>(t, t), kvp => kvp.Key))
		{
		}

		public new bool Add(T item) => _wrappedCollection.TryAdd(item, item);
		public IReadOnlySet<T> GetSnapshot() => _wrappedCollection.Keys;
	}
}
