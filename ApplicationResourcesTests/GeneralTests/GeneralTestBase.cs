using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using SpotifyProject.Setup;
using SpotifyProject.Utils.GeneralUtils;

namespace SpotifyProjectTests.GeneralTests
{
	public class GeneralTestBase : UnitTestBase
	{
		private readonly static object _lock = new object();
		private static bool _isLoaded = false;

		[OneTimeSetUp]
		public void OneTimeSetUp__GeneralTestBase()
		{
			Utils.LoadOnce(ref _isLoaded, _lock, () =>
			{
				if (File.Exists(Constants.StandardSettingsFile))
					Settings.RegisterProvider(new XmlSettingsProvider(Constants.StandardSettingsFile));
				else
					throw new FileNotFoundException($"In order to run unit tests, you must provide general settings in a file located at {Constants.StandardSettingsFile}");
				Settings.Load();
			});
		}
	}
}
