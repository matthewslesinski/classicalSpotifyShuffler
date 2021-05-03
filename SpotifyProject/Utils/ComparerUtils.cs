using System;
using System.Collections.Generic;

namespace SpotifyProject.Utils
{
	public static class ComparerUtils
	{
		public static IComparer<T> ComparingBy<T, R>(Func<T, R> mappingFunction, IComparer<R> resultComparer) =>
			Comparer<T>.Create((o1, o2) => resultComparer.Compare(mappingFunction(o1), mappingFunction(o2)));

		public static IComparer<T> ComparingBy<T>(Func<T, IComparable> mappingFunction) =>
			ComparingBy(mappingFunction, Comparer<IComparable>.Default);

		public static IComparer<T> ThenBy<T>(this IComparer<T> outerComparer, IComparer<T> innerComparer) =>
			Comparer<T>.Create((o1, o2) =>
			{
				var firstResult = outerComparer.Compare(o1, o2);
				return firstResult == 0 ? innerComparer.Compare(o1, o2) : firstResult;
			});

		public static IComparer<T> ThenBy<T>(this IComparer<T> outerComparer, Func<T, IComparable> mappingFunction) =>
			ThenBy(outerComparer, ComparingBy(mappingFunction));

		public static IComparer<T> Reverse<T>(this IComparer<T> comparer) =>
			Comparer<T>.Create((o1, o2) => comparer.Compare(o2, o1));

	}
}
