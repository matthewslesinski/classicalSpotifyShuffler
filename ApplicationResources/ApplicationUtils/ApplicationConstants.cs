using System;
using ApplicationResources.Utils;
using CustomResources.Utils.Concepts;

namespace ApplicationResources.ApplicationUtils
{
	public static class ApplicationConstants
	{
		public const string StandardSettingsFile = "standardSettings.xml";
		public const string StandardUnitTestSettingsFile = "standardUnitTestSettings.xml";
		public const string XmlSettingsFileFlag = "--settingsXmlFile";
	}

	public static class ApplicationConstants<T>
	{
		public readonly static Bijection<T, string> JSONSerializer = new Bijection<T, string>(t => t.ToJsonString(), GeneralExtensions.FromJsonString<T>);
	}
}
