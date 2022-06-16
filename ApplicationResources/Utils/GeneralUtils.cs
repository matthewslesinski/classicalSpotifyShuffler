using System;
using System.IO;
using System.Linq;
namespace ApplicationResources.Utils
{
	public static class GeneralUtils
	{
		public static string GetAbsoluteCombinedPath(string start, params string[] parts) =>
			parts.Aggregate(start, (arg1, arg2) => string.IsNullOrWhiteSpace(arg1) ? arg2 : Path.GetFullPath(CombinePaths(arg1, arg2)));

		public static string CombinePaths(string part1, string part2)
		{
			if (string.IsNullOrWhiteSpace(part1))
				return part2;
			if (string.IsNullOrWhiteSpace(part2))
				return part1;
			return Path.Combine(part1, part2);
		}
	}
}

