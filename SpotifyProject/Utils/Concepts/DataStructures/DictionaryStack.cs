using System;
using System.Collections.Generic;
using SpotifyProject.Utils.Extensions;

namespace SpotifyProject.Utils.Concepts.DataStructures
{
	public class DictionaryStack<K, V> : Stack<IDictionary<K, V>>
	{
		public IDisposable NewScope(IDictionary<K, V> dict)
		{
			Push(dict);
			return new StackScope(this, dict);
		}

		public IDisposable ModifyScope(K key, V newValue)
		{
			if (!TryPeek(out var topElement) || topElement.IsReadOnly)
				throw new InvalidOperationException($"The stack is empty or the top element is readonly");
			var containsOldValue = topElement.TryGetValue(key, out var oldValue);
			topElement[key] = newValue;
			return new ItemScope(this, topElement, key, containsOldValue, oldValue, newValue);
		}

		public bool TryGetTopValue(K key, out V foundValue) => this.TryGetFirst((IDictionary<K, V> dict, out V value) => dict.TryGetValue(key, out value), out foundValue);


		private class StackScope : IDisposable
		{
			protected readonly IDictionary<K, V> _topElement;
			protected readonly DictionaryStack<K, V> _stack;

			internal StackScope(DictionaryStack<K, V> stack, IDictionary<K, V> topElement)
			{
				_topElement = topElement;
				_stack = stack;
			}

			public virtual void Dispose()
			{
				if (!_stack.TryPeek(out var topElement) || !ReferenceEquals(topElement, _topElement))
					throw new InvalidOperationException($"The top element of the stack was previously removed when it shouldn't have been. The stack currently has {_stack.Count} elements");
				_stack.Pop();
			}
		}

		private class ItemScope : StackScope
		{
			private readonly K _key;
			private readonly bool _oldValueExists;
			private readonly V _oldValue;
			private readonly V _newValue;

			internal ItemScope(DictionaryStack<K, V> stack, IDictionary<K, V> topElement, K key, bool oldValueExists, V oldValue, V newValue) : base(stack, topElement)
			{
				_key = key;
				_oldValueExists = oldValueExists;
				_oldValue = oldValue;
				_newValue = newValue;
			}

			public override void Dispose()
			{
				if (!_stack.TryPeek(out var topElement) || !ReferenceEquals(topElement, _topElement)
					|| !topElement.TryGetValue(_key, out var foundValue) || !ReferenceEquals(foundValue, _newValue))
					throw new InvalidOperationException($"The top element of the stack was previously removed or modified out of order. " +
						$"The dictionary should be on the stack and have value {_newValue} associated with key {_key}. There are currently {_stack.Count} items on the stack");
				if (_oldValueExists)
					topElement[_key] = _oldValue;
				else
					topElement.Remove(_key);
			}
		}
	}
}
