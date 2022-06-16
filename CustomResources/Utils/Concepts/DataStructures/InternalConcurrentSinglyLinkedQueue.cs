using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using CustomResources.Utils.GeneralUtils;

namespace CustomResources.Utils.Concepts.DataStructures
{
    /// <summary>
    /// A Queue data structure that consists of a singly linked list, where the head is the front of the queue (next to be dequeued) and the tail is the
    /// back (latest to be added). The queue supports concurrent access, by consumers and producers in multiple threads. The queue works by maintaining
    /// a reference to both the head node and the tail. It is based off the algorithm for a non-blocking queue described in "Simple, Fast, and
	/// Practical Non-Blocking and Blocking Concurrent Queue Algorithms" (https://www.cs.rochester.edu/~scott/papers/1996_PODC_queues.pdf) by Maged
	/// Michael and Michael Scott. Because this is the same algorithm that Java's implementation of this same data strcuture was based off of,
	/// that implementation was also consulted in building this one to ensure correctness. Also, some of the optimizations and approaches that that
	/// implementation employs, such as allowing _head and _tail to lag to reduce compare-and-swap calls, were also included here.
	///
	/// Some details of the implementation: dummy nodes with null values are allowed in the queue. They are ignored and skipped over, and frequently
	/// removed when encountered, but useful to use as sentinel values, especially for head. This way, when deleting a value, setting the node's value
	/// to null will effectively remove it, but issues of linking the surrounding nodes to actually remove the node can be put off till appropriate.
	/// Similarly, after a node's value has been deleted logically, actually removing it from the queue frequently involves setting its _next pointer
	/// to point to itself, which is an indicator to any threads that may still be using it that it has been removed. Also, _tail and _head will
	/// deliberately not be updated after most add/removes until a second one also occurs (as mentioned above). This is an optimization that reduces
	/// the number of calls to compare-and-swap.
	///
	/// The reason for implementing this class instead of using the build-in ConcurrentQueue implementation is because this implementation provides
	/// the TryDequeueIf method, which allows for evaluating the first element before deciding whether to dequeue it. With the ConcurrentQueue implementation,
	/// doing the same functionality would require peeking at the first element, evaluating the function on it, and then dequeueing it. However, this
	/// is not thread safe, since another thread could have dequeued the element in between the peek and the dequeue actions. 
    /// </summary>
    /// <typeparam name="T">The type of elements in the queue</typeparam>
    public class InternalConcurrentSinglyLinkedQueue<T> : IProducerConsumerCollection<T>, IConcurrentCollection<T, T[]>, IReadOnlyCollection<T>
    {
        private class Node
        {
            // Use a Reference as a way of boxing the type as a reference type to allow using null
            internal Reference<T> _item;
            internal Node _next;

            internal Node(Reference<T> item)
            {
                _item = item;
            }
        }

        private Node _head;
        private Node _tail;

		public InternalConcurrentSinglyLinkedQueue()
        {
            _head = new Node(null);
            _tail = _head;
        }

        public InternalConcurrentSinglyLinkedQueue(IEnumerable<T> c)
        {
            var (start, end) = MakeSegmentToAdd(c);
            if (start == null)
                start = end = new Node(null);

            _head = start;
            _tail = end;
        }

        public int Count
        {
            get
            {
                var count = 0;
                for (var curr = GetFirstNodeAndItem().node; curr != null; curr = GetNextNode(curr))
                    if (curr._item != null)
                        count += 1;
                return count;
            }
        }

        public bool IsSynchronized => false;
		public object SyncRoot => throw new NotSupportedException("Concurrent collections don't need a SyncRoot to externally synchronize them");
        public bool IsReadOnly => false;

		public bool Contains(T searchItem) => Contains(searchItem, null);
        public bool Contains(T searchItem, IEqualityComparer<T> equalityComparer)
        {
            if (searchItem == null) return false;
            equalityComparer ??= EqualityComparer<T>.Default;
            for (var curr = GetFirstNodeAndItem().node; curr != null; curr = GetNextNode(curr))
            {
                var item = curr._item;
                if (item != null && equalityComparer.Equals(searchItem, item))
                    return true;
            }
            return false;
        }


        public bool TryAdd(T item) { Enqueue(item); return true; }
        public void Add(T item) => Enqueue(item);
        public void Enqueue(T item)
        {
            Ensure.ArgumentNotNull(item, nameof(item));
            var newNode = new Node(item);
            AddNodesAtEnd(newNode, newNode);
        }

        public bool AddAll(ICollection<T> collection)
        {
            if (collection == null || collection.Count == 0)
                return false;

            if (collection == this)
                throw new ArgumentException("Cannot add to the queue from itself", nameof(collection));

            var (segmentStart, end) = MakeSegmentToAdd(collection);
            AddNodesAtEnd(segmentStart, end);
            return true;
        }

        private void AddNodesAtEnd(Node segmentStart, Node end)
		{
            var multipleElementsToAdd = !ReferenceEquals(segmentStart, end);

            var currentTail = _tail;
            var candidatePrev = currentTail;
            while (true)
            {
                var currentNext = candidatePrev._next;
                if (currentNext == null)
                {
                    if (Interlocked.CompareExchange(ref candidatePrev._next, segmentStart, null) == null)
                    {
                        if (multipleElementsToAdd)
                        {
                            if (Interlocked.CompareExchange(ref _tail, end, currentTail) != currentTail)
                            {
                                currentTail = _tail;
                                if (end._next == null)
                                    Interlocked.CompareExchange(ref _tail, end, currentTail);
                            }
                            return;
                        }
                        else
                        {
                            if (candidatePrev != currentTail)
                                Interlocked.CompareExchange(ref _tail, end, currentTail);
                            return;
                        }
                    }
                }
                else if (candidatePrev == currentNext)
                {
                    var tailHasBeenUpdatedElsewhere = currentTail != (currentTail = _tail);
                    candidatePrev = tailHasBeenUpdatedElsewhere
                        ? currentTail
                        : _head;
                }
                else
                {
                    var shouldReturnToTailInsteadOfNext = candidatePrev != currentTail && currentTail != (currentTail = _tail);
                    candidatePrev = shouldReturnToTailInsteadOfNext
                        ? currentTail
                        : currentNext;
                }
            }
        }

        private static (Node start, Node end) MakeSegmentToAdd(IEnumerable<T> sequence)
        {
            Node start = null;
            Node end = null;
            foreach (T item in sequence)
            {
                if (item != null)
                {
                    var newNode = new Node(item);
                    if (start == null)
                        start = end = newNode;
                    else
                    {
                        end = newNode;
                        end._next = newNode;
                    }
                }
            }
            return (start, end);
        }

        public bool TryPeek(out T foundItem)
        {
            var (_, nodeItem) = GetFirstNodeAndItem();
            foundItem = nodeItem;
            return foundItem != null;
        }

        public bool Remove(T toRemove)
        {
            if (toRemove != null)
            {
                var currentNode = GetFirstNodeAndItem().node;
                Node previousNode = null;
                Node nextNode;
                while (currentNode != null)
                {
                    var item = currentNode._item;
                    if (item == null || Equals(toRemove, item))
                    {
                        var removed = item != null && Interlocked.CompareExchange(ref currentNode._item, null, item) == item;
                        nextNode = GetNextNode(currentNode);

                        if (previousNode != null && nextNode != null)
                            Interlocked.CompareExchange(ref previousNode._next, nextNode, currentNode);
                        if (removed)
                            return true;
                    }
                    else
                        nextNode = GetNextNode(currentNode);


                    previousNode = currentNode;
                    currentNode = nextNode;
                }
            }
            return false;
        }

        public void Clear()
        {
            var newDummy = new Node(null);
            newDummy._next = newDummy;
            Interlocked.Exchange(ref _head, newDummy);
            Interlocked.Exchange(ref _tail, _head);
            Interlocked.Exchange(ref newDummy._next, null);
        }

        public bool TryTake(out T removedItem) => TryDequeue(out removedItem);
        public bool TryDequeue(out T removedItem) => TryDequeueIf(_ => true, out removedItem);
        public bool TryDequeueIf(Func<T, bool> shouldRemovePredicate, out T removedItem) => TryDequeueIf(shouldRemovePredicate, out _, out removedItem);
        private bool TryDequeueIf(Func<T, bool> shouldRemovePredicate, out Node removedNode, out T removedItem)
        {
            Ensure.ArgumentNotNull(shouldRemovePredicate, nameof(shouldRemovePredicate));
            while (true)
            {
                var currentHead = _head;
                var currentNode = currentHead;
                Node nextNode;
                while (true)
                {
                    var item = currentNode._item;
                    var shouldRemove = item != null && shouldRemovePredicate(item);

                    if (shouldRemove && Interlocked.CompareExchange(ref currentNode._item, null, item) == item)
                    {
                        if (currentNode != currentHead)
                            UpdateHead(currentHead, ((nextNode = currentNode._next) != null) ? nextNode : currentNode);
                        removedNode = currentNode;
                        removedItem = item;
                        return true;
                    }
                    else if (item != null && !shouldRemove)
					{
                        UpdateHead(currentHead, currentNode);
                        removedNode = currentNode;
                        removedItem = item;
                        return false;
					}
                    else if ((nextNode = currentNode._next) == null)
                    {
                        UpdateHead(currentHead, currentNode);
                        removedNode = currentNode;
                        removedItem = default;
                        return false;
                    }
                    else if (currentNode == nextNode)
                        break;
                    else
                        currentNode = nextNode;
                }
            }
        }

        private void UpdateHead(Node providedOldHead, Node providedNewHead)
        {
            if (providedOldHead != providedNewHead && Interlocked.CompareExchange(ref _head, providedNewHead, providedOldHead) == providedOldHead)
                providedOldHead._next = providedOldHead;
        }

        private Node GetNextNode(Node node)
        {
            var next = node._next;
            return (node == next) ? _head : next;
        }

		private (Node node, T nodeItem) GetFirstNodeAndItem()
        {
            TryDequeueIf(_ => false, out var foundNode, out var foundItem);
            return (foundNode, foundItem);
        }

        void IProducerConsumerCollection<T>.CopyTo(T[] array, int index) => ((ICollection<T>) this).CopyTo(array, index);

        public T[] GetSnapshot() => ToArray();
        public T[] ToArray()
        {
            var accumulator = new List<T>();
            for (var currentNode = GetFirstNodeAndItem().node; currentNode != null; currentNode = GetNextNode(currentNode))
            {
                var item = currentNode._item;
                if (item != null)
                    accumulator.Add(item);
            }
            return accumulator.ToArray();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);
        }

		private class Enumerator : IEnumerator<T>
        {
            private readonly InternalConcurrentSinglyLinkedQueue<T> _container;
            private Node _currentNode;
            private bool _hasStarted = false;

            internal Enumerator(InternalConcurrentSinglyLinkedQueue<T> container)
            {
                _container = container;
            }

            object IEnumerator.Current => Current;
            public T Current { get; private set; }

            public bool MoveNext()
            {
                var currentNode = !_hasStarted ? _container.GetFirstNodeAndItem().node : _container.GetNextNode(_currentNode);
                _hasStarted = true;
                while (true)
                {
                    _currentNode = currentNode;
                    if (currentNode == null)
                    {
                        Current = default;
                        return false;
                    }
                    var item = currentNode._item;
                    if (item != null)
                    {
                        Current = item;
                        return true;
                    }
                    else
                        currentNode = _container.GetNextNode(currentNode);
                }
            }

            public void Reset()
            {
                Current = default;
                _hasStarted = false;
            }

            public void Dispose() { }
        }
    }
}