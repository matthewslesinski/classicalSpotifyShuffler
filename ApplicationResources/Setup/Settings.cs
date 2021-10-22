using System;
using System.Collections.Generic;
using System.Linq;
using ApplicationResources.Logging;
using CustomResources.Utils.Extensions;
using Util = CustomResources.Utils.GeneralUtils.Utils;

namespace ApplicationResources.Setup
{
	public class Settings
	{
		private readonly static Dictionary<BasicSettings, object> _parsedSettings = new Dictionary<BasicSettings, object>();
		private readonly static List<ISettingsProvider<BasicSettings>> _settingsProviders = new List<ISettingsProvider<BasicSettings>>();
		private static readonly object _loadLock = new object();
		private static bool _isLoaded = false;

		public static IEnumerable<(BasicSettings setting, string stringValue)> GetAllSettingsAsStrings() =>
			Enum.GetValues<BasicSettings>().Select(setting => (setting, _parsedSettings.TryGetValue(setting, out var parsedSettingValue) 
																			? setting.GetExtension<ISettingsSpecification>().StringFormatter(parsedSettingValue)
																			: null));

		public static T Get<T>(BasicSettings setting) => TryGet<T>(setting, out var value) ? value : default;
		public static bool TryGet<T>(BasicSettings setting, out T value)
		{
			try
			{
				var foundValue = _parsedSettings.TryGetValue(setting, out var uncastedValue);
				value = !foundValue || uncastedValue == null ? default : (T) uncastedValue;
				return foundValue;
			} catch (InvalidCastException e)
			{
				var message = $"Could not retrieve setting {setting} because its value is of the wrong type. Attempting to cast to type {typeof(T).Name}. {e}";
				Console.Error.WriteLine(message);
				Logger.Error(message);
				value = default;
				return false;
			}
		}

		public static void RegisterProvider(ISettingsProvider<BasicSettings> provider) => _settingsProviders.Add(provider);
		public static void RegisterProviders(params ISettingsProvider<BasicSettings>[] providers) => RegisterProviders(providers);
		public static void RegisterProviders(IEnumerable<ISettingsProvider<BasicSettings>> providers) => _settingsProviders.AddRange(providers);

		public static void Load()
		{
			Util.LoadOnce(ref _isLoaded, _loadLock, () =>
			{
				_settingsProviders.Each(provider =>
				{
					try
					{
						if (!provider.IsLoaded)
							provider.Load();
					}
					catch (Exception e)
					{
						Logger.Error($"An exception occurred while trying to load provider of type {provider.GetType().Name}: {e}");
					}
				});
				var exceptions = new List<Exception>();
				foreach (var settingName in Enum.GetValues<BasicSettings>())
				{
					var specification = settingName.GetExtension<ISettingsSpecification>();
					var didFindValue = _settingsProviders.Where(provider => provider != null && provider.IsLoaded)
						.TryGetFirst((ISettingsProvider<BasicSettings> provider, out IEnumerable<string> values) => provider.TryGetValues(settingName, out values), out var foundValues);
					if (didFindValue)
						_parsedSettings[settingName] = ParseSetting(settingName, foundValues);
					else if (specification.IsRequired)
						exceptions.Add(new KeyNotFoundException($"A value for setting {settingName} is required but nothing was provided"));
					else if (specification.Default != null)
						_parsedSettings[settingName] = specification.Default;
				}
				if (exceptions.Any())
				{
					var exception = exceptions.Count() > 1 ? new AggregateException(exceptions) : exceptions.First();
					throw exception;
				}
				GetAllSettingsAsStrings().Each(kvp => Logger.Verbose("Setting {settingName} was set to value {settingValue}", kvp.setting, kvp.stringValue ?? "<null>"));
			});
		}

		private static object ParseSetting(BasicSettings setting, IEnumerable<string> rawValueStrings)
		{
			var specification = setting.GetExtension<ISettingsSpecification>();
			var parsedValues = specification.ValueGetter(rawValueStrings);
			return parsedValues;
		}
	}

	public interface ISettingsSpecification
	{
		bool IsRequired { get; set; }
		object Default { get; set; }
		Func<IEnumerable<string>, object> ValueGetter { get; set; }
		Func<object, string> StringFormatter { get; set; }
	}

	public interface ISettingsProvider<KeyType>
	{
		public void Load();
		public bool IsLoaded { get; }
		public bool TryGetValues(KeyType setting, out IEnumerable<string> values);
	}
}
