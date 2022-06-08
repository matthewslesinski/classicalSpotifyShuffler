using System;
using System.IO;
using System.Linq;
namespace ApplicationResources.Utils
{
	public static class GeneralUtils
	{
		public static string GetAbsoluteCombinedPath(string start, params string[] parts) =>
			parts.Aggregate(start, (arg1, arg2) => Path.GetFullPath(Path.Combine(arg1, arg2)));
	}
}

