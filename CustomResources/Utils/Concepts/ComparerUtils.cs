using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using CustomResources.Utils.Extensions;

namespace CustomResources.Utils.Concepts
{
	public static class ComparerUtils
	{
		public static KeyBasedComparer<T, R> ComparingBy<T, R>(Func<T, R> mappingFunction, IComparer<R> resultComparer) =>
			new KeyBasedComparer<T, R>(mappingFunction, resultComparer);

		public static KeyBasedComparer<T, IComparable> ComparingBy<T>(Func<T, IComparable> mappingFunction) =>
			ComparingBy(mappingFunction, Comparer<IComparable>.Default);

		public static IComparer<T> ThenBy<T>(this IComparer<T> outerComparer, IComparer<T> innerComparer) =>
			Comparer<T>.Create((o1, o2) =>
			{
				var firstResult = outerComparer.Compare(o1, o2);
				return firstResult == 0 ? innerComparer.Compare(o1, o2) : firstResult;
			});

		public static IComparer<T> ThenBy<T>(this IComparer<T> outerComparer, Func<T, IComparable> mappingFunction) =>
			ThenBy(outerComparer, ComparingBy(mappingFunction));

		public static IComparer<T> Reversed<T>(this IComparer<T> comparer) =>
			Comparer<T>.Create((o1, o2) => comparer.Compare(o2, o1));

		public static bool Equals<T>(this IComparer<T> comparer, T a, T b) => comparer.Compare(a, b) == 0;
		public static bool GreaterThan<T>(this IComparer<T> comparer, T a, T b) => comparer.Compare(a, b) > 0;
		public static bool GreaterThanOrEqual<T>(this IComparer<T> comparer, T a, T b) => comparer.Compare(a, b) >= 0;
		public static bool LessThan<T>(this IComparer<T> comparer, T a, T b) => comparer.Compare(a, b) < 0;
		public static bool LessThanOrEqual<T>(this IComparer<T> comparer, T a, T b) => comparer.Compare(a, b) <= 0;

	}

	public static class EqualityUtils
	{
		public static IEqualityComparer<T> EqualBy<T, R>(Func<T, R> mappingFunction, IEqualityComparer<R> resultComparer) =>
			new KeyBasedEqualityComparer<T, R>(mappingFunction, resultComparer);

		public static IEqualityComparer<T> EqualBy<T, R>(Func<T, R> mappingFunction) => EqualBy(mappingFunction, EqualityComparer<R>.Default);

	}

	public class SimpleEqualityComparer<T> : IEqualityComparer<T>
	{
		protected readonly Func<T, T, bool> _equalityFunction;
		protected readonly Func<T, int> _hashFunction;
		public SimpleEqualityComparer(Func<T, T, bool> equalityFunction, Func<T, int> hashFunction)
		{
			_equalityFunction = equalityFunction;
			_hashFunction = hashFunction;
		}

		public bool Equals([AllowNull] T x, [AllowNull] T y)
		{
			return _equalityFunction(x, y);
		}

		public int GetHashCode([DisallowNull] T obj)
		{
			return _hashFunction(obj);
		}
	}

	public class KeyBasedEqualityComparer<T, R> : SimpleEqualityComparer<T>
	{
		public KeyBasedEqualityComparer(Func<T, R> mappingFunction, IEqualityComparer<R> resultEqualityComparer = null)
			: base((x, y) => (resultEqualityComparer ?? EqualityComparer<R>.Default).Equals(mappingFunction(x), mappingFunction(y)),
				  obj => (resultEqualityComparer ?? EqualityComparer<R>.Default).GetHashCode(mappingFunction(obj)))
		{ }
	}

	public class KeyBasedComparer<T, R> : KeyBasedEqualityComparer<T, R>, IComparer<T>
	{
		private readonly IComparer<R> _resultComparer;
		private readonly Func<T, R> _mappingFunction;
		public KeyBasedComparer(Func<T, R> mappingFunction, IComparer<R> resultComparer = null, IEqualityComparer<R> resultEqualityComparer = null)
			: base(mappingFunction, resultEqualityComparer)
		{
			_resultComparer = resultComparer ?? Comparer<R>.Default;
			_mappingFunction = mappingFunction;
		}

		public int Compare([AllowNull] T x, [AllowNull] T y)
		{
			return _resultComparer.Compare(_mappingFunction(x), _mappingFunction(y));
		}
	}

	internal delegate bool ToleranceCompareFunc<T>(T x, T y, T delta);

	public class ToleranceComparer<T> : IComparer<T>, IEqualityComparer<T>
	{
		private static readonly IEqualityComparer<T> _hashCodeProvider = EqualityComparer<T>.Default;
		private static readonly ToleranceCompareFunc<T> _toleranceFunc = (ToleranceCompareFunc<T>) ToleranceComparers.FuncsForTypes.AddIfNotPresent(typeof(T), t => GetToleranceFuncForType());

		private readonly T _delta;
		private readonly IComparer<T> _comparer;

		public ToleranceComparer(T delta, IComparer<T> comparer = null)
		{
			_comparer = comparer ?? Comparer<T>.Default;
			_delta = delta;
		}

		public int GetHashCode([DisallowNull] T obj)
		{
			return _hashCodeProvider.GetHashCode(obj);
		}

		public bool Equals([AllowNull] T x, [AllowNull] T y)
		{
			return _hashCodeProvider.Equals(x, y) || _toleranceFunc(x, y, _delta);
		}

		public int Compare([AllowNull] T x, [AllowNull] T y)
		{
			return Equals(x, y) ? 0 : _comparer.Compare(x, y);
		}

		private static ToleranceCompareFunc<T> GetToleranceFuncForType() {
			if (ToleranceComparers.TryCreateToleranceFuncForType(out ToleranceCompareFunc<T> toleranceFunc))
				return toleranceFunc;
			else if (typeof(T).IsAssignableTo(typeof(IConvertible)) && ToleranceComparers.FuncsForTypes.TryGetCastedValue(typeof(decimal), out ToleranceCompareFunc<decimal> decimalToleranceFunc))
				return (i, j, delta) => decimalToleranceFunc(Convert.ToDecimal(i), Convert.ToDecimal(j), Convert.ToDecimal(delta));
			throw new ArgumentException($"Cannot create a tolerance comparer for type {typeof(T).Name}");
		}
	}

	internal static class ToleranceComparers
	{
		internal static readonly IDictionary<Type, Delegate> FuncsForTypes = new Dictionary<Type, Delegate>
		{
			{ typeof(int), (ToleranceCompareFunc<int>) ((i, j, delta) => Math.Abs( i -  j) <= delta) },
			{ typeof(double), (ToleranceCompareFunc<double>) ((i, j, delta) => Math.Abs(i - j) <= delta) },
			{ typeof(decimal), (ToleranceCompareFunc<decimal>) ((i, j, delta) => Math.Abs(i - j) <= delta) },
			{ typeof(long), (ToleranceCompareFunc<long>) ((i, j, delta) => Math.Abs(i - j) <= delta) },
		};

		internal static bool TryCreateToleranceFuncForType<T>(out ToleranceCompareFunc<T> toleranceFunc)
		{
			try
			{
				var type = typeof(T);
				MethodInfo absMethodInfo = typeof(Math).GetMethod(nameof(Math.Abs), new[] { type });
				if (absMethodInfo == null)
				{
					toleranceFunc = default;
					return false;
				}
				var iParam = Expression.Parameter(typeof(T));
				var jParam = Expression.Parameter(typeof(T));
				var deltaParam = Expression.Parameter(typeof(T));
				var subtraction = Expression.Subtract(iParam, jParam);
				var absCall = Expression.Call(null, absMethodInfo, subtraction);
				var comparison = Expression.LessThanOrEqual(absCall, deltaParam);
				toleranceFunc = Expression.Lambda<ToleranceCompareFunc<T>>(comparison, iParam, jParam, deltaParam).Compile();
				return true;
			}
			catch (InvalidOperationException)
			{
				toleranceFunc = default;
				return false;
			}
		}
	}
}
