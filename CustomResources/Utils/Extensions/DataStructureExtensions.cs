using System;
using System.Collections;
using System.Collections.Generic;
using SpotifyProject.Utils.Concepts;

namespace SpotifyProject.Utils.Extensions
{
	public static class DataStructureExtensions
	{
		public static V AddIfNotPresent<K, V>(this IDictionary<K, V> dictionary, K key, Func<V> valueSupplier) => AddIfNotPresent(dictionary, key, _ => valueSupplier());
		public static V AddIfNotPresent<K, V>(this IDictionary<K, V> dictionary, K key, Func<K, V> valueSupplier) =>
			dictionary.TryGetValue(key, out var foundValue) ? foundValue : (dictionary[key] = valueSupplier(key));

		public static void AddOrReplace<T>(this IList<T> list, int index, T element) { if (index < list.Count) list[index] = element; else list.Add(element); }
		public static int BinarySearch<K, V>(this SortedList<K, V> list, K queryItem) => list.Keys.BinarySearch(queryItem, list.Comparer);
		public static int BinarySearch<T>(this IList<T> list, T queryItem, IComparer<T> comparer = null) => GeneralUtils.Utils.BinarySearch(i => list[i], 0, list.Count, queryItem, comparer);

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
	}
}
