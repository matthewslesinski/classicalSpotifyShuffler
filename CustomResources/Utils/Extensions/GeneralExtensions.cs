using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CustomResources.Utils.Concepts.DataStructures;
using CustomResources.Utils.GeneralUtils;

namespace CustomResources.Utils.Extensions
{
	public static class GeneralExtensions
	{
		#region Function Concatenation
		public static Func<T, V> AndThen<T, U, V>(this Func<T, U> func, Func<U, V> continuation)
		{
			Ensure.ArgumentNotNull(func, nameof(func));
			Ensure.ArgumentNotNull(continuation, nameof(continuation));
			return t => continuation(func(t));
		}

		public static Func<T, Task<V>> AndThenAsync<T, U, V>(this Func<T, Task<U>> func, Func<U, V> continuation)
		{
			Ensure.ArgumentNotNull(func, nameof(func));
			Ensure.ArgumentNotNull(continuation, nameof(continuation));
			return async t => continuation(await func(t).WithoutContextCapture());
		}

		public static Func<R> AndThen<T, R>(this Func<T> func, Func<T, R> continuation)
		{
			Ensure.ArgumentNotNull(func, nameof(func));
			Ensure.ArgumentNotNull(continuation, nameof(continuation));
			return () => continuation(func());
		}

		public static Func<Task<R>> AndThenAsync<T, R>(this Func<Task<T>> func, Func<T, R> continuation)
		{
			Ensure.ArgumentNotNull(func, nameof(func));
			Ensure.ArgumentNotNull(continuation, nameof(continuation));
			return async () => continuation(await func().WithoutContextCapture());
		}

		public static Func<R> AndThen<R>(this Action action, Func<R> func)
		{
			Ensure.ArgumentNotNull(action, nameof(action));
			Ensure.ArgumentNotNull(func, nameof(func));
			return () => { action(); return func(); };
		}

		public static Func<Task<R>> AndThenAsync<R>(this Func<Task> action, Func<R> func)
		{
			Ensure.ArgumentNotNull(action, nameof(action));
			Ensure.ArgumentNotNull(func, nameof(func));
			return async () => { await action().WithoutContextCapture(); return func(); };
		}

		public static Func<T, R> AndThen<T, R>(this Action action, Func<T, R> func)
		{
			Ensure.ArgumentNotNull(action, nameof(action));
			Ensure.ArgumentNotNull(func, nameof(func));
			return t => { action(); return func(t); };
		}

		public static Func<R> AndThen<R>(this Func<R> func, Action action)
		{
			Ensure.ArgumentNotNull(action, nameof(action));
			Ensure.ArgumentNotNull(func, nameof(func));
			return () => { var r = func(); action(); return r; };
		}

		public static Func<Task> AndThenAsync(this Func<Task> func, Action action)
		{
			Ensure.ArgumentNotNull(action, nameof(action));
			Ensure.ArgumentNotNull(func, nameof(func));
			return async () => { await func().WithoutContextCapture(); action(); };
		}

		public static Func<Task<R>> AndThenAsync<R>(this Func<Task<R>> func, Action action)
		{
			Ensure.ArgumentNotNull(action, nameof(action));
			Ensure.ArgumentNotNull(func, nameof(func));
			return async () => { var r = await func().WithoutContextCapture(); action(); return r; };
		}

		public static Func<T, R> AndThen<T, R>(this Func<T, R> func, Action action)
		{
			Ensure.ArgumentNotNull(action, nameof(action));
			Ensure.ArgumentNotNull(func, nameof(func));
			return t => { var r = func(t); action(); return r; };
		}

		public static Func<T, Task> AndThenAsync<T>(this Func<T, Task> func, Action action)
		{
			Ensure.ArgumentNotNull(action, nameof(action));
			Ensure.ArgumentNotNull(func, nameof(func));
			return async t => { await func(t).WithoutContextCapture(); action(); return; };
		}

		public static Func<T, Task<R>> AndThenAsync<T, R>(this Func<T, Task<R>> func, Action action)
		{
			Ensure.ArgumentNotNull(action, nameof(action));
			Ensure.ArgumentNotNull(func, nameof(func));
			return async t => { var r = await func(t).WithoutContextCapture(); action(); return r; };
		}

		public static Func<T, Task> FollowedByAsync<T>(this Func<T, Task> func, Func<Task> action)
		{
			Ensure.ArgumentNotNull(action, nameof(action));
			Ensure.ArgumentNotNull(func, nameof(func));
			return async t => { await func(t).WithoutContextCapture(); await action().WithoutContextCapture(); };
		}

		public static Func<T, Task<R>> FollowedByAsync<T, R>(this Func<T, Task> func, Func<Task<R>> action)
		{
			Ensure.ArgumentNotNull(action, nameof(action));
			Ensure.ArgumentNotNull(func, nameof(func));
			return async t => { await func(t).WithoutContextCapture(); return await action().WithoutContextCapture(); };
		}

		public static Func<T, Task> FollowedByAsync<T>(this Func<T, Task> func, Func<T, Task> followUpFunc)
		{
			Ensure.ArgumentNotNull(followUpFunc, nameof(followUpFunc));
			Ensure.ArgumentNotNull(func, nameof(func));
			return async t => { await func(t).WithoutContextCapture(); await followUpFunc(t).WithoutContextCapture(); };
		}

		public static Func<T, Task<R>> FollowedByAsync<T, R>(this Func<T, Task> func, Func<T, Task<R>> followUpFunc)
		{
			Ensure.ArgumentNotNull(followUpFunc, nameof(followUpFunc));
			Ensure.ArgumentNotNull(func, nameof(func));
			return async t => { await func(t).WithoutContextCapture(); return await followUpFunc(t).WithoutContextCapture(); };
		}
		#endregion

		public static (A first, B second, C third) Append<A, B, C>(this (A first, B second) firstTwo, C third) => (firstTwo.first, firstTwo.second, third);
		public static (A first, B second, C third, D fourth) Append<A, B, C, D>(this (A first, B second, C third) firstThree, D fourth) => (firstThree.first, firstThree.second, firstThree.third, fourth);
		public static (A first, B second, C third, D fourth, E fifth) Append<A, B, C, D, E>(this (A first, B second, C third, D fourth) firstFour, E fifth) => (firstFour.first, firstFour.second, firstFour.third, firstFour.fourth, fifth);

		public static R As<R>(this R obj) where R : class => obj;
		public static R AsUnsafe<R>(this object obj) => obj is R casted ? casted : throw new InvalidCastException($"Cannot cast object from {obj.GetType().Name} to {typeof(R).Name}");

		public static bool AsBool(this int num) => num != 0;
		public static int AsInt(this bool b) => b ? 1 : 0;

		public static IEnumerable<T> AsIEnumerable<T>(this T item) => (SingleEnumerable<T>) item;

		public static IEnumerable<DelegateT> GetAllCalls<DelegateT>(this DelegateT multiDelegate) where DelegateT : Delegate =>
			multiDelegate == null ? Array.Empty<DelegateT>() : multiDelegate.GetInvocationList().Cast<DelegateT>();

		public static A GetFirst<A, B>(this (A item1, B item2) tuple) => tuple.item1;
		public static B GetSecond<A, B>(this (A item1, B item2) tuple) => tuple.item2;

		public static string Replace(this string str, Regex regex, string replacement) => str == null ? null : regex.Replace(str, replacement);

		public static bool SupportsNullValues(this Type type) => !type.IsValueType || (Nullable.GetUnderlyingType(type) != null);

		public static ConfiguredTaskAwaitable WithoutContextCapture(this Task task) =>
			task.ConfigureAwait(continueOnCapturedContext: false);
		public static ConfiguredTaskAwaitable<V> WithoutContextCapture<V>(this Task<V> task) =>
			task.ConfigureAwait(continueOnCapturedContext: false);
		public static ConfiguredValueTaskAwaitable<V> WithoutContextCapture<V>(this ValueTask<V> task) =>
			task.ConfigureAwait(continueOnCapturedContext: false);
		public static ConfiguredCancelableAsyncEnumerable<T> WithoutContextCapture<T>(this IAsyncEnumerable<T> sequence) =>
			sequence.ConfigureAwait(continueOnCapturedContext: false);
	}
}