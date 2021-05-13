using System;
using System.Collections.Generic;

namespace SpotifyProject.Utils
{
    public static class ArrayExtensions
    {
		public static void Fill<T>(this T[] arr, IEnumerable<T> enumerable)
		{
			var i = 0;
			foreach (var item in enumerable)
			{
				if (i == arr.Length)
				{
					throw new ArgumentException($"Enumerable exceeded array length {arr.Length}");
				}
				arr[i++] = item;
			}
		}
    }
}