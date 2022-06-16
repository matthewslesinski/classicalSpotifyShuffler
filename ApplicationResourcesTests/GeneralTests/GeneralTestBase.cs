using System;
using System.IO;
using System.Threading.Tasks;
using ApplicationResources.ApplicationUtils;
using NUnit.Framework;
using ApplicationResources.Setup;
using CustomResources.Utils.GeneralUtils;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.Concepts.DataStructures;

namespace ApplicationResourcesTests.GeneralTests
{
	public class GeneralTestBase : UnitTestBase
	{
		private static readonly AsyncLockProvider _lock = new();
		private static readonly MutableReference<bool> _isLoaded = new(false);

		[OneTimeSetUp]
		public Task OneTimeSetUp__GeneralTestBase()
		{
			return Utils.LoadOnceBlockingAsync(_isLoaded, _lock, async (_) =>
			{
				if (File.Exists(ApplicationConstants.StandardSettingsFile))
					await Settings.RegisterProvider(new XmlSettingsProvider(ApplicationConstants.StandardSettingsFile)).WithoutContextCapture();
				else
					throw new FileNotFoundException($"In order to run unit tests, you must provide general settings in a file located at {ApplicationConstants.StandardSettingsFile}");
				await Settings.Load().WithoutContextCapture();
			});
		}
	}
}
