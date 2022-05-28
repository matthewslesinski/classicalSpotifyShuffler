using System;
using System.IO;
using System.Threading.Tasks;
using ApplicationResources.ApplicationUtils;
using NUnit.Framework;
using ApplicationResources.Setup;
using CustomResources.Utils.GeneralUtils;

namespace ApplicationResourcesTests.GeneralTests
{
	public class GeneralTestBase : UnitTestBase
	{
		private readonly static object _lock = new object();
		private static bool _isLoaded = false;

		[OneTimeSetUp]
		public void OneTimeSetUp__GeneralTestBase()
		{
			Utils.LoadOnceBlocking(ref _isLoaded, _lock, () =>
			{
				if (File.Exists(ApplicationConstants.StandardSettingsFile))
					Settings.RegisterProvider(new XmlSettingsProvider(ApplicationConstants.StandardSettingsFile));
				else
					throw new FileNotFoundException($"In order to run unit tests, you must provide general settings in a file located at {ApplicationConstants.StandardSettingsFile}");
				Settings.Load();
			});
		}
	}
}
