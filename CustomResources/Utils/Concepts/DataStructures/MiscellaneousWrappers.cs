using System;
using System.Collections;
using System.Collections.Generic;
using CustomResources.Utils.GeneralUtils;

namespace CustomResources.Utils.Concepts.DataStructures
{
	public struct Enumerable<T> : IEnumerable<T>
	{
		private readonly Func<IEnumerator<T>> _enumeratorSupplier;

		public Enumerable(IEnumerator<T> enumerator) : this(() => enumerator) { }
		public Enumerable(Func<IEnumerator<T>> enumeratorSupplier) => _enumeratorSupplier = enumeratorSupplier;

		public IEnumerator<T> GetEnumerator() => _enumeratorSupplier();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	public struct SingleEnumerable<T> : IEnumerable<T>
	{
		private readonly T _item;

		public SingleEnumerable(T item) => _item = item;

		public IEnumerator<T> GetEnumerator() => new SingleEnumerator<T>(_item);
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public static explicit operator SingleEnumerable<T>(T item) => new SingleEnumerable<T>(item);
	}

	public struct SingleEnumerator<T> : IEnumerator<T>
	{
		private readonly T _value;
		private int step = 0;
		public SingleEnumerator(T obj)
		{
			_value = obj;
		}

		object IEnumerator.Current => Current;
		public T Current => step == 1 ? _value : default;
		public bool MoveNext() => step++ == 1;
		public void Dispose() { }
		public void Reset() => step = 0;
	}

	public class CollectionAsReadOnly<T> : IGenericInternalCollection<T>
	{
		protected readonly ICollection<T> _wrappedCollection;
		public CollectionAsReadOnly(ICollection<T> wrappedCollection)
		{
			Ensure.ArgumentNotNull(wrappedCollection, nameof(wrappedCollection));
			_wrappedCollection = wrappedCollection;
		}

		public int Count => _wrappedCollection.Count;
		public bool IsReadOnly => _wrappedCollection.IsReadOnly;
		public void Add(T item) => _wrappedCollection.Add(item);
		public void Clear() => _wrappedCollection.Clear();
		public bool Contains(T item) => _wrappedCollection.Contains(item);
		public IEnumerator<T> GetEnumerator() => _wrappedCollection.GetEnumerator();
		public bool Remove(T item) => _wrappedCollection.Remove(item);
	}

	public class ReadOnlyAsFullCollection<T> : ReadOnlyCollectionWrapper<T, T>, IGenericInternalCollection<T>
	{
		public delegate bool ContainsImplementation(T item);

		private readonly ContainsImplementation _containsImplementation;

		public ReadOnlyAsFullCollection(IReadOnlyCollection<T> wrappedCollection, ContainsImplementation containsImplementation)
			: base(wrappedCollection, Bijections<T>.Identity.Function)
		{
			Ensure.ArgumentNotNull(containsImplementation, nameof(containsImplementation));
			_containsImplementation = containsImplementation;
		}

		public bool IsReadOnly => true;

		public void Add(T item) => throw new NotSupportedException(ModificationAttemptExceptionMessage);

		public void Clear() => throw new NotSupportedException(ModificationAttemptExceptionMessage);

		public bool Contains(T item) => _containsImplementation(item);

		public bool Remove(T item) => throw new NotSupportedException(ModificationAttemptExceptionMessage);

		private const string ModificationAttemptExceptionMessage = "This collection does not support modification";
	}
}
