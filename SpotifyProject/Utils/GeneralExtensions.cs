using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SpotifyProject.Utils
{
	public static class GeneralExtensions
	{
		public static A GetFirst<A, B>(this (A item1, B item2) tuple) => tuple.item1;
		public static B GetSecond<A, B>(this (A item1, B item2) tuple) => tuple.item2;

		public static string ToJsonString(this object obj) => JsonConvert.SerializeObject(obj);
		
		public static string Replace(this string str, Regex regex, string replacement) => str == null ? null : regex.Replace(str, replacement);

		public static ConfiguredTaskAwaitable WithoutContextCapture(this Task task) => 
			task.ConfigureAwait(continueOnCapturedContext: false);
		public static ConfiguredTaskAwaitable<V> WithoutContextCapture<V>(this Task<V> task) => 
			task.ConfigureAwait(continueOnCapturedContext: false);
		public static ConfiguredValueTaskAwaitable<V> WithoutContextCapture<V>(this ValueTask<V> task) => 
			task.ConfigureAwait(continueOnCapturedContext: false);
	}
}
