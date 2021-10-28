using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using CustomResources.Utils.Extensions;

namespace CustomResources.Utils.Concepts.DataStructures
{
	public class InternalConcurrentDictionary<K, V> : ConcurrentDictionary<K, V>, IElementContainer<K>, IReadOnlyDictionary<K, V>, IDictionary<K, V>
	{

		public InternalConcurrentDictionary(IEqualityComparer<K> keyComparer = null) : base(keyComparer ?? EqualityComparer<K>.Default)
		{
			EqualityComparer = keyComparer ?? EqualityComparer<K>.Default;
		}

		public new ISet<K> Keys => new HashSet<K>(base.Keys, EqualityComparer);
		IEnumerable<K> IReadOnlyDictionary<K, V>.Keys => Keys;
		ICollection<K> IDictionary<K, V>.Keys => Keys;

		public IEqualityComparer<K> EqualityComparer { get; }
	}
}
