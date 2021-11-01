using System;
using CustomResources.Utils.GeneralUtils;

namespace CustomResources.Utils.Extensions
{
	public static class EnumExtensions
	{
		public static ExtensionT GetExtension<ExtensionT>(this Enum enumValue) =>
			EnumExtenders<ExtensionT>.GetEnumExtension(enumValue);
	}
}
