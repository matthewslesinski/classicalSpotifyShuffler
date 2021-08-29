using System;
using Newtonsoft.Json;

namespace ApplicationResources.Utils
{
	public static class GeneralExtensions
	{
		public static T FromJsonString<T>(this string jsonString) => JsonConvert.DeserializeObject<T>(jsonString);
		public static string ToJsonString(this object obj) => JsonConvert.SerializeObject(obj);
	}
}
