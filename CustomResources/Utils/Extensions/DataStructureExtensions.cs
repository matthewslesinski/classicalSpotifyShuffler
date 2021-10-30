using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CustomResources.Utils.Concepts;
using CustomResources.Utils.Concepts.DataStructures;

namespace CustomResources.Utils.Extensions
{
	public static class DataStructureExtensions
	{
		public static V AddIfNotPresent<K, V>(this IDictionary<K, V> dictionary, K key, Func<V> valueSupplier) => AddIfNotPresent(dictionary, key, _ => valueSupplier());
		public static V AddIfNotPresent<K, V>(this IDictionary<K, V> dictionary, K key, Func<K, V> valueSupplier) =>
			dictionary.TryGetValue(key, out var foundValue) ? foundValue : (dictionary[key] = valueSupplier(key));

		public static void AddOrReplace<T>(this IList<T> list, int index, T element) { if (index < list.Count) list[index] = element; else list.Add(element); }
		public static int BinarySearch<K, V>(this SortedList<K, V> list, K queryItem) => list.Keys.BinarySearch(queryItem, list.Comparer);
		public static int BinarySearch<T>(this IList<T> list, T queryItem, IComparer<T> comparer = null) => GeneralUtils.Utils.BinarySearch(i => list[i], 0, list.Count, queryItem, comparer);

		public static bool NotContains<T>(this ICollection<T> collection, T item) => !collection.Contains(item);
		public static bool NotContainsKey<K, V>(this IReadOnlyDictionary<K, V> dict, K key) => !dict.ContainsKey(key);

		public static bool TryGetCeiling<K, V>(this SortedList<K, V> list, K queryItem, out K ceiling) => TryGetNeighbor(list, queryItem, true, true, out ceiling);
		public static bool TryGetFloor<K, V>(this SortedList<K, V> list, K queryItem, out K floor) => TryGetNeighbor(list, queryItem, false, true, out floor);
		public static bool TryGetNext<K, V>(this SortedList<K, V> list, K queryItem, out K next) => TryGetNeighbor(list, queryItem, true, false, out next);
		public static bool TryGetPrevious<K, V>(this SortedList<K, V> list, K queryItem, out K previous) => TryGetNeighbor(list, queryItem, false, false, out previous);
		private static bool TryGetNeighbor<K, V>(SortedList<K, V> list, K queryItem, bool lookAbove, bool allowQueryAsResult, out K neighbor)
		{
			var count = list.Count;
			var index = list.BinarySearch(queryItem);
			if (index >= 0)
			{
				if (allowQueryAsResult)
				{
					neighbor = list.Keys[index];
					return true;
				}
				else if (lookAbove)
					index += 1;
			}
			else
				index = ~index;
			if (count == 0 || (lookAbove && index >= count) || (!lookAbove && index <= 0))
			{
				neighbor = default;
				return false;
			}
			neighbor = list.Keys[lookAbove ? index : index - 1];
			return true;
		}

		public static bool TryGetCeiling<T>(this SortedSet<T> set, T queryItem, out T ceiling) => TryGetNeighbor(set, queryItem, true, true, out ceiling);
		public static bool TryGetFloor<T>(this SortedSet<T> set, T queryItem, out T floor) => TryGetNeighbor(set, queryItem, false, true, out floor);
		public static bool TryGetNext<T>(this SortedSet<T> set, T queryItem, out T next) => TryGetNeighbor(set, queryItem, true, false, out next);
		public static bool TryGetPrevious<T>(this SortedSet<T> set, T queryItem, out T previous) => TryGetNeighbor(set, queryItem, false, false, out previous);
		private static bool TryGetNeighbor<T>(SortedSet<T> set, T queryItem, bool lookAbove, bool allowQueryAsResult, out T neighbor)
		{
			T setMin = set.Min;
			T setMax = set.Max;
			if (set.Count == 0 || (lookAbove && set.Comparer.GreaterThan(queryItem, setMax)) || (!lookAbove && set.Comparer.LessThan(queryItem, setMin)))
			{
				neighbor = default;
				return false;
			}
			var view = set.GetViewBetween(lookAbove ? queryItem : set.Min, lookAbove ? set.Max : queryItem);
			using var enumerator = (lookAbove ? view : view.Reverse()).GetEnumerator();
			if (!enumerator.MoveNext() || (!allowQueryAsResult && set.Comparer.Equals(queryItem, enumerator.Current) && !enumerator.MoveNext()))
			{
				neighbor = default;
				return false;
			}
			neighbor = enumerator.Current;
			return true;
		}

		public static bool TryGetCastedValue<K, V>(this IDictionary<K, object> dictionary, K key, out V value) => TryGetCastedValue<K, object, V>(dictionary, key, out value);
		public static bool TryGetCastedValue<K, O, V>(this IDictionary<K, O> dictionary, K key, out V value) where V : O
		{
			if (dictionary.TryGetValue(key, out var foundValue) && foundValue is V castedValue)
			{
				value = castedValue;
				return true;
			}
			value = default;
			return false;
		}

		public static bool TryGetValues<K, V>(this ILookup<K, V> lookup, K key, out IEnumerable<V> values)
		{
			if (lookup.Contains(key))
			{
				values = lookup[key];
				return true;
			}
			values = Array.Empty<V>();
			return false;
		}

		#region Collection Wrapper Creators

		public static ReadOnlyCollectionWrapper<T, WrappedT> SelectAsReadOnlyCollection<T, WrappedT>(this IReadOnlyCollection<WrappedT> collection, Func<WrappedT, T> selector) =>
			new ReadOnlyCollectionWrapper<T, WrappedT>(collection, selector);

		public static CollectionWrapper<T, WrappedT, CollectionT> SelectAsCollection<T, WrappedT, CollectionT>(this CollectionT collection, Bijection<WrappedT, T> selector)
			where CollectionT : ICollection<WrappedT>, IReadOnlyCollection<WrappedT>, ICollection =>
				new CollectionWrapper<T, WrappedT, CollectionT>(collection, selector.Invert());

		public static SetWrapper<T, WrappedT, SetT> SelectAsSet<T, WrappedT, SetT>(this SetT set, Bijection<WrappedT, T> selector, IEqualityComparer<WrappedT> equalityComparer = null)
			where SetT : ISet<WrappedT>, IReadOnlySet<WrappedT>, ICollection =>
				new SetWrapper<T, WrappedT, SetT>(set, selector.Invert(), equalityComparer);

		public static ReadOnlyDictionaryWrapper<K1, V1, K2, V2, DictT> SelectAsDictionary<K1, V1, K2, V2, DictT>(this DictT dictionary, Bijection<K2, K1> keySelector,
			Func<V2, V1> valueSelector, IEqualityComparer<K2> keyEqualityComparer = null) where DictT : IReadOnlyDictionary<K2, V2>, ICollection =>
				new ReadOnlyDictionaryWrapper<K1, V1, K2, V2, DictT>(dictionary, keySelector.Invert(), valueSelector, keyEqualityComparer);

		public static DictionaryWrapper<K1, V1, K2, V2, DictT> SelectAsDictionary<K1, V1, K2, V2, DictT>(this DictT dictionary, Bijection<K2, K1> keySelector,
			Bijection<V2, V1> valueSelector, IEqualityComparer<K2> keyEqualityComparer = null) where DictT : IDictionary<K2, V2>, IReadOnlyDictionary<K2, V2>, ICollection =>
				new DictionaryWrapper<K1, V1, K2, V2, DictT>(dictionary, keySelector.Invert(), valueSelector.Invert(), keyEqualityComparer);

		public static ReadOnlyCollectionFilter<T, IReadOnlyCollection<T>> WhereAsCollection<T>(this IReadOnlyCollection<T> collection, Func<T, bool> filter) =>
			new ReadOnlyCollectionFilter<T, IReadOnlyCollection<T>>(collection, filter);

		public static ReadOnlyDictionaryFilter<K, V, DictT> WhereAsDictionary<K, V, DictT>(this DictT dictionary, Func<K, bool> keyFilter = null,
			Func<V, bool> valueFilter = null, IEqualityComparer<K> keyEqualityComparer = null) where DictT : IReadOnlyDictionary<K, V>, ICollection =>
				new ReadOnlyDictionaryFilter<K, V, DictT>(dictionary, keyFilter, valueFilter, keyEqualityComparer);

		#endregion
	}
}
