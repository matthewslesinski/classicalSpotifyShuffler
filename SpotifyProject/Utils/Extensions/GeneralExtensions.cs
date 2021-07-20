using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SpotifyProject.Utils.Extensions
{
	public static class GeneralExtensions
	{

		public static (A first, B second, C third) Append<A, B, C>(this (A first, B second) firstTwo, C third) => (firstTwo.first, firstTwo.second, third);
		public static (A first, B second, C third, D fourth) Append<A, B, C, D>(this (A first, B second, C third) firstThree, D fourth) => (firstThree.first, firstThree.second, firstThree.third, fourth);
		public static (A first, B second, C third, D fourth, E fifth) Append<A, B, C, D, E>(this (A first, B second, C third, D fourth) firstFour, E fifth) => (firstFour.first, firstFour.second, firstFour.third, firstFour.fourth, fifth);

		public static A GetFirst<A, B>(this (A item1, B item2) tuple) => tuple.item1;
		public static B GetSecond<A, B>(this (A item1, B item2) tuple) => tuple.item2;

		public static string Replace(this string str, Regex regex, string replacement) => str == null ? null : regex.Replace(str, replacement);
		
		public static string ToJsonString(this object obj) => JsonConvert.SerializeObject(obj);

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

		public static ConfiguredTaskAwaitable WithoutContextCapture(this Task task) => 
			task.ConfigureAwait(continueOnCapturedContext: false);
		public static ConfiguredTaskAwaitable<V> WithoutContextCapture<V>(this Task<V> task) => 
			task.ConfigureAwait(continueOnCapturedContext: false);
		public static ConfiguredValueTaskAwaitable<V> WithoutContextCapture<V>(this ValueTask<V> task) => 
			task.ConfigureAwait(continueOnCapturedContext: false);
	}
}
