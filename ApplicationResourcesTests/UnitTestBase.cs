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
using ApplicationResources.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using CustomResources.Utils.Concepts.DataStructures;
using ApplicationResources.ApplicationUtils.Parameters;
using System.Threading;

namespace ApplicationResourcesTests
{
	public abstract class UnitTestBase
	{
		private static readonly AsyncLockProvider _lock = new();
		private static readonly MutableReference<bool> _isLoaded = new(false);

		[OneTimeSetUp]
		public static async Task OneTimeSetUp__UnitTestBase()
		{
			var settingsFiles = new[] { ApplicationConstants.StandardUnitTestSettingsFile, ApplicationConstants.StandardSettingsFile };
			await Utils.LoadOnceBlockingAsync(_isLoaded, _lock, async (_) =>
			{
				GlobalDependencies.Initialize().AddGlobalService<IDataStoreAccessor, FileAccessor>().Build();
				await TaskParameters.Initialize().WithoutContextCapture();
				Settings.RegisterSettings<BasicSettings>();
				await LoadSettingsFiles(false, ApplicationConstants.StandardUnitTestSettingsFile, ApplicationConstants.StandardSettingsFile).WithoutContextCapture();
			}).WithoutContextCapture();
		}

		[SetUp]
		public void SetUp__UnitTestBase() { }

		[TearDown]
		public void TearDown__UnitTestBase()
		{
			if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
				Logger.Error("Test: {testName} failed with message {failureMessage}", TestContext.CurrentContext.Test.FullName, TestContext.CurrentContext.Result.Message);
		}

		protected static async Task LoadSettingsFiles(bool giveHighestPriority, params string[] settingsFiles)
		{
			var localData = GlobalDependencies.Get<IDataStoreAccessor>();
			Func<IEnumerable<ISettingsProvider>, CancellationToken, Task> registerAction = giveHighestPriority ? Settings.RegisterHighestPriorityProviders : Settings.RegisterProviders;
			var checkedSettingsFiles = (await settingsFiles
				.SelectAsync<string, (string fileName, bool exists)>(async fileName => (fileName, await localData.ExistsAsync(fileName).WithoutContextCapture()))
				.ToList().WithoutContextCapture()).ToLookup(tup => tup.exists, tup => tup.fileName);
			if (checkedSettingsFiles.TryGetValues(true, out var existingFiles))
				await registerAction(existingFiles.Select(filename => new XmlSettingsProvider(filename)), default).WithoutContextCapture();
			if (checkedSettingsFiles.TryGetValues(false, out var missingFiles))
				missingFiles.EachIndependently(filename => throw new FileNotFoundException($"In order to run unit tests, you must provide general settings in a file located at {filename}"));
		}
	}
}