using System;
using System.Collections;
using System.Collections.Generic;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;

namespace CustomResources.Utils.Concepts.DataStructures
{
	public class CollectionAsReadOnly<T> : ICollection<T>, IReadOnlyCollection<T>
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
		public void CopyTo(T[] array, int arrayIndex) => _wrappedCollection.CopyTo(array, arrayIndex);
		public IEnumerator<T> GetEnumerator() => _wrappedCollection.GetEnumerator();
		public bool Remove(T item) => _wrappedCollection.Remove(item);
		IEnumerator IEnumerable.GetEnumerator() => _wrappedCollection.As<IEnumerable>().GetEnumerator();
	}

	public sealed class DisposableAction : IDisposable
	{
		private readonly Action _disposeAction;
		public DisposableAction(Action disposeAction)
		{
			Ensure.ArgumentNotNull(disposeAction, nameof(disposeAction));
			_disposeAction = disposeAction;
		}

		public void Dispose() => _disposeAction();
	}
}
