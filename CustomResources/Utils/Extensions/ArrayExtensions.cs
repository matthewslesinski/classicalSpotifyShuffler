using System;
using System.Collections.Generic;

namespace CustomResources.Utils.Extensions
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

		public static T Get<T>(this T[] arr, int index) => arr[index];

		public static void Swap<T>(this T[] array, int index1, int index2)
		{
			var temp = array[index1];
			array[index1] = array[index2];
			array[index2] = temp;
		}
	}
}