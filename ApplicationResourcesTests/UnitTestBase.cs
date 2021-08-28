using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using SpotifyProject.Setup;
using SpotifyProject.Utils.GeneralUtils;
using NUnit.Framework.Interfaces;
using SpotifyProject;
using System.Collections.Generic;

namespace SpotifyProjectTests
{
	public abstract class UnitTestBase
	{
		private readonly static object _lock = new object();
		private static bool _isLoaded = false;

		[OneTimeSetUp]
		public void OneTimeSetUp__UnitTestBase()
		{
			string unitTestSettingsFileName = ApplicationConstants.StandardUnitTestSettingsFile;
			Utils.LoadOnce(ref _isLoaded, _lock, () =>
			{
				if (File.Exists(unitTestSettingsFileName))
					Settings.RegisterProvider(new XmlSettingsProvider(unitTestSettingsFileName));
				else
					throw new FileNotFoundException($"In order to run unit tests, you must provide settings in a file located at {unitTestSettingsFileName}");
			});
		}

		[SetUp]
		public void SetUp__UnitTestBase() { }

		[TearDown]
		public void TearDown__UnitTestBase()
		{
			if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
				Logger.Error("Test: {testName} failed with message {failureMessage}", TestContext.CurrentContext.Test.FullName, TestContext.CurrentContext.Result.Message);
		}
	}
}
