using System;
using System.Collections.Generic;
using System.Linq;
using ApplicationResources.Setup;
using CustomResources.Utils.GeneralUtils;
using SpotifyProject.SpotifyUtils;
using SpotifyProject.Utils;
using static ApplicationResources.Setup.CommandLineSettingsProvider;

namespace SpotifyProject.Configuration
{
	[EnumExtensionProvider(typeof(SpotifySettingsSpecifications))]
	[EnumExtensionProvider(typeof(SpotifySettingsCommandLineSpecifications))]
	public enum SpotifySettings
	{
		TrackQueueSizeLimit,
		AskUser,
		HTTPLoggerName,
		MetadataRecordFile,
		NumHTTPConnections,
		APIRateLimitWindow,
		APIRateLimitStatsFile,
		PersonalDataDirectory,
	}

	public class SpotifySettingsSpecifications : IEnumExtensionProvider<SpotifySettings, ISettingSpecification>
	{
		public IReadOnlyDictionary<SpotifySettings, ISettingSpecification> Specifications { get; } = new Dictionary<SpotifySettings, ISettingSpecification>
		{
			{ SpotifySettings.MetadataRecordFile,                new StringSettingSpecification() },
			{ SpotifySettings.HTTPLoggerName,                    new StringSettingSpecification() },
			{ SpotifySettings.PersonalDataDirectory,             new StringSettingSpecification() },
			{ SpotifySettings.APIRateLimitStatsFile,             new StringSettingSpecification { Default = GeneralConstants.SuggestedAPIRateLimitStatsFile } },
			{ SpotifySettings.AskUser,                           new BoolSettingSpecification() },
			{ SpotifySettings.APIRateLimitWindow,                new ParameterSpecification<TimeSpan> { Default = TimeSpan.FromMilliseconds(SpotifyConstants.APIRateLimitWindowMS), ValueGetter = values => TimeSpan.FromMilliseconds(int.Parse(values.Single())), Validator = timeSpan => timeSpan > TimeSpan.FromSeconds(5) } },
			{ SpotifySettings.TrackQueueSizeLimit,               new ConvertibleSettingSpecification<int> { Default = 750} },
			{ SpotifySettings.NumHTTPConnections,                new ConvertibleSettingSpecification<int> { ValueGetter = values => int.TryParse(values.Single(), out var numConnections) && numConnections > 0 ? numConnections : int.MaxValue, Default = int.MaxValue } },
		};
	}

	public class SpotifySettingsCommandLineSpecifications : IEnumExtensionProvider<SpotifySettings, ICommandLineSpecification>
	{
		public IReadOnlyDictionary<SpotifySettings, ICommandLineSpecification> Specifications { get; } = new Dictionary<SpotifySettings, ICommandLineSpecification>
		{
			{ SpotifySettings.TrackQueueSizeLimit,             new SingleValueOption { Flag = "-q|--queueSizeLimit", Desc = "The cap on the number of tracks to send in a request to create a new queue" } },
			{ SpotifySettings.APIRateLimitWindow,              new SingleValueOption { Flag = "-q|--apiRateLimitWindow", Desc = "The number of milliseconds that the Spotify API keeps requests contributing towards the rate limit" } },
			{ SpotifySettings.PersonalDataDirectory,           new SingleValueOption { Flag = "-q|--personalDataDirectory", Desc = "The file directory where personal data files are stored" } },
			{ SpotifySettings.APIRateLimitStatsFile,           new SingleValueOption { Flag = "-q|--apiRateLimitStatsFile", Desc = "The filename for where to store calculated stats about the Spotify API's rate limit" } },
			{ SpotifySettings.AskUser,                         new NoValueOption     { Flag = "--askUser", Desc = "Provide if the user should be asked what context to reorder" } },
			{ SpotifySettings.HTTPLoggerName,                  new SingleValueOption { Flag = "--httpLogger", Desc = "The name of the http logger to be used." } },
			{ SpotifySettings.MetadataRecordFile,              new SingleValueOption { Flag = "--metadataRecordFile", Desc = "The location to write input to LukesTrackLinker for unit tests" } },
			{ SpotifySettings.NumHTTPConnections,              new SingleValueOption { Flag = "--numHttpConnections", Desc = "The number of http connections to spotify's api to allow" } }
		};
	}
}
