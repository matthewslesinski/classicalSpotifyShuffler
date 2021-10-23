using System;
using System.Collections;
using System.Collections.Generic;
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

	public abstract class CollectionWrapper<T, WrappedElementT, CollectionT> : ReadOnlyCollectionWrapper<T, WrappedElementT, CollectionT>, IInternalCollection<T>
		where CollectionT : ICollection<WrappedElementT>, IReadOnlyCollection<WrappedElementT>
	{
		protected readonly Func<T, WrappedElementT> _mappingFunction;

		protected CollectionWrapper(CollectionT wrappedCollection, Bijection<T, WrappedElementT> mappingFunction) : base(wrappedCollection, mappingFunction.Inverse)
		{
			Ensure.ArgumentNotNull(mappingFunction, nameof(mappingFunction));
			_mappingFunction = mappingFunction.Function;
		}

		public override bool IsReadOnly => _wrappedCollection.IsReadOnly;

		public virtual void Add(T item) => _wrappedCollection.Add(_mappingFunction.Invoke(item));

		public virtual void Clear() => _wrappedCollection.Clear();

		public virtual bool Contains(T item) => _wrappedCollection.Contains(_mappingFunction.Invoke(item));


		public virtual bool Remove(T item) => _wrappedCollection.Remove(_mappingFunction.Invoke(item));
	}

	public abstract class CollectionWrapper<T, CollectionT> : CollectionWrapper<T, T, CollectionT> where CollectionT : ICollection<T>, IReadOnlyCollection<T>
	{
		protected CollectionWrapper(CollectionT wrappedCollection) : base(wrappedCollection, Bijections<T>.Identity) { }
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
}
