using System;
using System.Collections.Generic;
using System.Linq;
using SpotifyProject.Utils;

namespace SpotifyProject.Setup
{
	public enum SettingsName
	{
		ClientInfoPath,
		TokenPath,
		RedirectUri,
		DefaultToAlbumShuffle,
		ArtistAlbumIncludeGroups,
		TrackQueueSizeLimit,
		MaintainCurrentlyPlaying,
		ConsoleLogLevel,
		OutputFileLogLevel,
		LogFileName,
		AskUser,
		TransformationName,
		HTTPLoggerName,
		RetryHandlerName,
		PaginatorName,
		APIConnectorName,
		RandomSeed,
		MetadataRecordFile,
		PlaybackSetterName,
		SaveAsPlaylistName,
		SupplyUserInput,
		SpotifyProjectRootDirectory,
		NumHTTPConnections,
		HTTPLoggerCharacterLimit,
	}

	public class Settings
	{
		private readonly static IReadOnlyDictionary<SettingsName, SettingsSpecification> _settingsSpecifications = new Dictionary<SettingsName, SettingsSpecification>
		{
			{ SettingsName.MetadataRecordFile,                new SettingsSpecification() },
			{ SettingsName.LogFileName,						  new SettingsSpecification() },
			{ SettingsName.TransformationName,                new SettingsSpecification() },
			{ SettingsName.HTTPLoggerName,					  new SettingsSpecification() },
			{ SettingsName.RetryHandlerName,                  new SettingsSpecification() },
			{ SettingsName.PaginatorName,					  new SettingsSpecification() },
			{ SettingsName.APIConnectorName,				  new SettingsSpecification() },
			{ SettingsName.PlaybackSetterName,                new SettingsSpecification() },
			{ SettingsName.SaveAsPlaylistName,                new SettingsSpecification() },
			{ SettingsName.TokenPath,                         new SettingsSpecification() },
			{ SettingsName.SpotifyProjectRootDirectory,       new SettingsSpecification { Default = Environment.CurrentDirectory } },
			{ SettingsName.RandomSeed,                        new SettingsSpecification { ValueGetter = values => int.Parse(values.First()) } },
			{ SettingsName.TrackQueueSizeLimit,               new SettingsSpecification { ValueGetter = values => int.Parse(values.First()), Default = 750} },
			{ SettingsName.NumHTTPConnections,                new SettingsSpecification { ValueGetter = values => int.TryParse(values.First(), out var numConnections) && numConnections > 0 ? numConnections : int.MaxValue, Default = int.MaxValue } },
			{ SettingsName.HTTPLoggerCharacterLimit,          new SettingsSpecification { ValueGetter = values => int.TryParse(values.First(), out var characterLimit) && characterLimit > 0 ? characterLimit : null, Default = null } },
			{ SettingsName.DefaultToAlbumShuffle,             new SettingsSpecification { ValueGetter = values => values.Any() && (!bool.TryParse(values.First(), out var parsedValue) || parsedValue) } },
			{ SettingsName.MaintainCurrentlyPlaying,          new SettingsSpecification { ValueGetter = values => values.Any() && (!bool.TryParse(values.First(), out var parsedValue) || parsedValue) } },
			{ SettingsName.AskUser,                           new SettingsSpecification { ValueGetter = values => values.Any() && (!bool.TryParse(values.First(), out var parsedValue) || parsedValue) } },
			{ SettingsName.ArtistAlbumIncludeGroups,          new SettingsSpecification { ValueGetter = values => values.First().Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(), StringFormatter = GeneralExtensions.ToJsonString } },
			{ SettingsName.ConsoleLogLevel,                   new SettingsSpecification { ValueGetter = values => Enum.Parse<LogLevel>(values.First(), true), Default = LogLevel.Info } },
			{ SettingsName.OutputFileLogLevel,                new SettingsSpecification { ValueGetter = values => Enum.Parse<LogLevel>(values.First(), true), Default = LogLevel.Verbose } },
			{ SettingsName.ClientInfoPath,                    new SettingsSpecification { IsRequired = true} },
			{ SettingsName.RedirectUri,                       new SettingsSpecification { IsRequired = true } },
			{ SettingsName.SupplyUserInput,                   new SettingsSpecification { ValueGetter = values => values, StringFormatter = GeneralExtensions.ToJsonString } }
		};

		private readonly static Dictionary<SettingsName, object> _parsedSettings = new Dictionary<SettingsName, object>();
		private readonly static List<ISettingsProvider> _settingsProviders = new List<ISettingsProvider>();
		private static readonly object _loadLock = new object();
		private static bool _isLoaded = false;

		public static IEnumerable<(SettingsName setting, string stringValue)> GetAllSettingsAsStrings() =>
			Enum.GetValues<SettingsName>().Select(setting => (setting, _parsedSettings.TryGetValue(setting, out var parsedSettingValue) 
																			? _settingsSpecifications[setting].StringFormatter(parsedSettingValue)
																			: null));

		public static T Get<T>(SettingsName setting) => TryGet<T>(setting, out var value) ? value : default;
		public static bool TryGet<T>(SettingsName setting, out T value)
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

		public static void RegisterProvider(ISettingsProvider providers) => _settingsProviders.Add(providers);
		public static void RegisterProviders(params ISettingsProvider[] providers) => RegisterProviders(providers);
		public static void RegisterProviders(IEnumerable<ISettingsProvider> providers) => _settingsProviders.AddRange(providers);

		public static void Load()
		{
			Utils.Utils.LoadOnce(ref _isLoaded, _loadLock, () =>
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
				foreach (var settingName in Enum.GetValues<SettingsName>())
				{
					var specification = _settingsSpecifications[settingName];
					var didFindValue = _settingsProviders.Where(provider => provider != null && provider.IsLoaded)
						.TryGetFirst((ISettingsProvider provider, out IEnumerable<string> values) => provider.TryGetValues(settingName, out values), out var foundValues);
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

		private static object ParseSetting(SettingsName setting, IEnumerable<string> rawValueStrings)
		{
			var specification = _settingsSpecifications[setting];
			var parsedValues = specification.ValueGetter(rawValueStrings);
			return parsedValues;
		}

		private class SettingsSpecification
		{
			internal bool IsRequired { get; set; } = false;
			internal object Default { get; set; } = null;
			internal Func<IEnumerable<string>, object> ValueGetter { get; set; }
				= rawValues => rawValues.TryGetFirst(out var foundResult) && !string.IsNullOrWhiteSpace(foundResult) ? foundResult : default;
			internal Func<object, string> StringFormatter { get; set; } = obj => obj?.ToString();
		}
	}

	public interface ISettingsProvider
	{
		public void Load();
		public bool IsLoaded { get; }
		public bool TryGetValues(SettingsName setting, out IEnumerable<string> values);
	}
}
