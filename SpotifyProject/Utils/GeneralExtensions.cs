using System;
using Newtonsoft.Json;

namespace SpotifyProject.Utils
{
	public static class GeneralExtensions
	{
		public static A GetFirst<A, B>(this (A item1, B item2) tuple) => tuple.item1;
		public static B GetSecond<A, B>(this (A item1, B item2) tuple) => tuple.item2;

		public static string ToJsonString(this object obj) => JsonConvert.SerializeObject(obj);

	}
}
