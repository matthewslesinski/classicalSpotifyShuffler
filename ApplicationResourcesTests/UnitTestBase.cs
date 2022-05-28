using System;
using System.IO;
using NUnit.Framework;
using ApplicationResources.Setup;
using CustomResources.Utils.GeneralUtils;
using NUnit.Framework.Interfaces;
using ApplicationResources.ApplicationUtils;
using ApplicationResources.Logging;
using System.Linq;
using CustomResources.Utils.Extensions;
using System.Collections.Generic;

namespace ApplicationResourcesTests
{
	public abstract class UnitTestBase
	{
		private readonly static object _lock = new object();
		private static bool _isLoaded = false;

		[OneTimeSetUp]
		public static void OneTimeSetUp__UnitTestBase()
		{
			var settingsFiles = new[] { ApplicationConstants.StandardUnitTestSettingsFile, ApplicationConstants.StandardSettingsFile };
			Utils.LoadOnceBlocking(ref _isLoaded, _lock, () =>
			{
				Settings.RegisterSettings<BasicSettings>();
				LoadSettingsFiles(false, ApplicationConstants.StandardUnitTestSettingsFile, ApplicationConstants.StandardSettingsFile);
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

		protected static void LoadSettingsFiles(bool giveHighestPriority, params string[] settingsFiles)
		{
			Action<IEnumerable<ISettingsProvider>> registerAction = giveHighestPriority ? Settings.RegisterHighestPriorityProviders : Settings.RegisterProviders;
			var checkedSettingsFiles = settingsFiles.ToLookup(File.Exists);
			if (checkedSettingsFiles.TryGetValues(true, out var existingFiles))
				registerAction(existingFiles.Select(filename => new XmlSettingsProvider(filename)));
			if (checkedSettingsFiles.TryGetValues(false, out var missingFiles))
				missingFiles.EachIndependently(filename => throw new FileNotFoundException($"In order to run unit tests, you must provide general settings in a file located at {filename}"));
		}
	}
}
