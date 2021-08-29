using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CustomResources.Utils.Extensions
{
	public static class GeneralExtensions
	{
		public static Func<T, V> AndThen<T, U, V>(this Func<T, U> first, Func<U, V> second) => t => second(first(t));

		public static (A first, B second, C third) Append<A, B, C>(this (A first, B second) firstTwo, C third) => (firstTwo.first, firstTwo.second, third);
		public static (A first, B second, C third, D fourth) Append<A, B, C, D>(this (A first, B second, C third) firstThree, D fourth) => (firstThree.first, firstThree.second, firstThree.third, fourth);
		public static (A first, B second, C third, D fourth, E fifth) Append<A, B, C, D, E>(this (A first, B second, C third, D fourth) firstFour, E fifth) => (firstFour.first, firstFour.second, firstFour.third, firstFour.fourth, fifth);

		public static R As<R>(this R obj) => obj;
		public static R As<R>(this object obj) => obj is R casted ? casted : throw new InvalidCastException($"Cannot cast object from {obj.GetType().Name} to {typeof(R).Name}");

		public static A GetFirst<A, B>(this (A item1, B item2) tuple) => tuple.item1;
		public static B GetSecond<A, B>(this (A item1, B item2) tuple) => tuple.item2;

		public static string Replace(this string str, Regex regex, string replacement) => str == null ? null : regex.Replace(str, replacement);
		
		public static ConfiguredTaskAwaitable WithoutContextCapture(this Task task) => 
			task.ConfigureAwait(continueOnCapturedContext: false);
		public static ConfiguredTaskAwaitable<V> WithoutContextCapture<V>(this Task<V> task) => 
			task.ConfigureAwait(continueOnCapturedContext: false);
		public static ConfiguredValueTaskAwaitable<V> WithoutContextCapture<V>(this ValueTask<V> task) => 
			task.ConfigureAwait(continueOnCapturedContext: false);
	}
}
