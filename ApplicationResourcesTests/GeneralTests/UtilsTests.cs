using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ApplicationResources.ApplicationUtils.Parameters;
using ApplicationResources.Setup;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;
using NUnit.Framework;

namespace ApplicationResourcesTests.GeneralTests
{
	public class UtilsTests : GeneralTestBase
	{


		[Test]
		public async Task TestSettingsOverrides()
		{
			var testSetting = BasicSettings.RandomSeed;
			var settingsStore = new SettingsStore();
			settingsStore.RegisterSettings(typeof(BasicSettings));
			await settingsStore.Load().WithoutContextCapture();
			Assert.IsFalse(settingsStore.TryGetValue(testSetting, out var _));
			using (settingsStore.AddOverrides((testSetting, 1), (testSetting, 2)))
			{
				Assert.IsTrue(settingsStore.TryGetValue(testSetting, out var overrideValue));
				Assert.AreEqual(2, overrideValue);
			}
			Assert.IsFalse(settingsStore.TryGetValue(testSetting, out var _));
		}

		[Test]
		public async Task TestParameterStore()
		{
			const string default1 = "default1";
			const string default2 = "default2";
			const string override1 = "override1";
			const string override3 = "override3";

			var testSettingStore = new SettingsStore();
			var testParameterStore = await ParameterStore.DerivedFrom(testSettingStore).WithoutContextCapture();
			testParameterStore.RegisterSettings(typeof(TestParameters));
			using var defaults = testSettingStore.AddOverrides((TestParameters.TestParameter1, default1), (TestParameters.TestParameter2, default2));
			await testParameterStore.Load().WithoutContextCapture();

			void CheckDefaults()
			{
				CollectionAssert.AreEquivalent(new[] { TestParameters.TestParameter1, TestParameters.TestParameter2 }, testParameterStore.LoadedSettings);
				Assert.AreEqual(default1, testParameterStore.TryGetValue(TestParameters.TestParameter1, out var testParameter1Value) ? testParameter1Value : default);
				Assert.AreEqual(default2, testParameterStore.TryGetValue(TestParameters.TestParameter2, out var testParameter2Value) ? testParameter2Value : default);
				Assert.IsFalse(testParameterStore.TryGetValue(TestParameters.TestParameter3, out var _));
			}

			async Task MainTask()
			{
				void CheckChanges()
				{
					CollectionAssert.AreEquivalent(new[] { TestParameters.TestParameter1, TestParameters.TestParameter2, TestParameters.TestParameter3 }, testParameterStore.LoadedSettings);
					Assert.AreEqual(override1, testParameterStore.TryGetValue(TestParameters.TestParameter1, out var testParameter1Value) ? testParameter1Value : default);
					Assert.AreEqual(default2, testParameterStore.TryGetValue(TestParameters.TestParameter2, out var testParameter2Value) ? testParameter2Value : default);
					Assert.AreEqual(override3, testParameterStore.TryGetValue(TestParameters.TestParameter3, out var testParameter3Value) ? testParameter3Value : default);
					Assert.AreEqual(default1, testSettingStore.TryGetValue(TestParameters.TestParameter1, out var testSetting1Value) ? testSetting1Value : default);
					Assert.IsFalse(testSettingStore.TryGetValue(TestParameters.TestParameter3, out var _));
				}
				CheckDefaults();
				using (testParameterStore.GetBuilder()
					.With(TestParameters.TestParameter1, override1)
					.With(TestParameters.TestParameter3, override3)
					.Apply())
				{
					CheckChanges();
					await Task.Delay(50);
					CheckChanges();
				}

				CheckDefaults();
			}

			var changeTask = MainTask();
			Thread.Sleep(25);
			CheckDefaults();
			await changeTask;
			CheckDefaults();
		}

		[EnumExtensionProvider(typeof(TestParametersSpecifications))]
		public enum TestParameters
		{
			TestParameter1,
			TestParameter2,
			TestParameter3
		}


		public class TestParametersSpecifications : IEnumExtensionProvider<TestParameters, IParameterSpecification>
		{
			public IReadOnlyDictionary<TestParameters, IParameterSpecification> Specifications { get; } = new Dictionary<TestParameters, IParameterSpecification>
			{
				{ TestParameters.TestParameter1,                new StringSettingSpecification() },
				{ TestParameters.TestParameter2,                new StringSettingSpecification() },
				{ TestParameters.TestParameter3,                new StringSettingSpecification() },
			};
		}
	}
}
