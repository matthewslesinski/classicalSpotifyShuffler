using System;
using Newtonsoft.Json;

namespace ApplicationResources.Utils
{
	public static class GeneralExtensions
	{
		public static string ToJsonString(this object obj) => JsonConvert.SerializeObject(obj);
	}
}
