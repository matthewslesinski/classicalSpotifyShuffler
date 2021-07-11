using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SpotifyProject.Utils
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
		internal KeyBasedEqualityComparer(Func<T, R> mappingFunction, IEqualityComparer<R> resultEqualityComparer = null)
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
}
