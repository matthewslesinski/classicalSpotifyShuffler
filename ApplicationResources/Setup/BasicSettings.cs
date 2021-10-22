using System;
using System.Collections.Generic;
using System.Linq;
using ApplicationResources.Logging;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;
using McMaster.Extensions.CommandLineUtils;
using static ApplicationResources.Setup.CommandLineOptions;
using ApplicationExtensions = ApplicationResources.Utils.GeneralExtensions;

namespace ApplicationResources.Setup
{
	[EnumExtensionProvider(typeof(SettingsSpecifications))]
	[EnumExtensionProvider(typeof(SettingsCommandLineSpecifications))]
	public enum BasicSettings
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
	public class SettingsSpecifications : IEnumExtensionProvider<BasicSettings, ISettingsSpecification>
	{
		public IReadOnlyDictionary<BasicSettings, ISettingsSpecification> Specifications { get; } = new Dictionary<BasicSettings, ISettingsSpecification>
		{
			{ BasicSettings.MetadataRecordFile,                new SettingsSpecification() },
			{ BasicSettings.LogFileName,                       new SettingsSpecification() },
			{ BasicSettings.TransformationName,                new SettingsSpecification() },
			{ BasicSettings.HTTPLoggerName,                    new SettingsSpecification() },
			{ BasicSettings.RetryHandlerName,                  new SettingsSpecification() },
			{ BasicSettings.PaginatorName,                     new SettingsSpecification() },
			{ BasicSettings.APIConnectorName,                  new SettingsSpecification() },
			{ BasicSettings.PlaybackSetterName,                new SettingsSpecification() },
			{ BasicSettings.SaveAsPlaylistName,                new SettingsSpecification() },
			{ BasicSettings.TokenPath,                         new SettingsSpecification() },
			{ BasicSettings.ClientInfoPath,                    new SettingsSpecification() },
			{ BasicSettings.RedirectUri,                       new SettingsSpecification() },
			{ BasicSettings.SpotifyProjectRootDirectory,       new SettingsSpecification { Default = Environment.CurrentDirectory } },
			{ BasicSettings.RandomSeed,                        new SettingsSpecification { ValueGetter = values => int.Parse(values.Single()) } },
			{ BasicSettings.TrackQueueSizeLimit,               new SettingsSpecification { ValueGetter = values => int.Parse(values.Single()), Default = 750} },
			{ BasicSettings.NumHTTPConnections,                new SettingsSpecification { ValueGetter = values => int.TryParse(values.Single(), out var numConnections) && numConnections > 0 ? numConnections : int.MaxValue, Default = int.MaxValue } },
			{ BasicSettings.HTTPLoggerCharacterLimit,          new SettingsSpecification { ValueGetter = values => int.TryParse(values.Single(), out var characterLimit) && characterLimit > 0 ? characterLimit : null, Default = null } },
			{ BasicSettings.DefaultToAlbumShuffle,             new SettingsSpecification { ValueGetter = values => !values.TryGetSingle(out var singleValue) || !bool.TryParse(singleValue, out var parsedValue) || parsedValue } },
			{ BasicSettings.MaintainCurrentlyPlaying,          new SettingsSpecification { ValueGetter = values => !values.TryGetSingle(out var singleValue) || !bool.TryParse(singleValue, out var parsedValue) || parsedValue } },
			{ BasicSettings.AskUser,                           new SettingsSpecification { ValueGetter = values => !values.TryGetSingle(out var singleValue) || !bool.TryParse(singleValue, out var parsedValue) || parsedValue } },
			{ BasicSettings.ArtistAlbumIncludeGroups,          new SettingsSpecification { ValueGetter = values => values.Single().Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(), StringFormatter = ApplicationExtensions.ToJsonString } },
			{ BasicSettings.ConsoleLogLevel,                   new SettingsSpecification { ValueGetter = values => Enum.Parse<LogLevel>(values.Single(), true), Default = LogLevel.Info } },
			{ BasicSettings.OutputFileLogLevel,                new SettingsSpecification { ValueGetter = values => Enum.Parse<LogLevel>(values.Single(), true), Default = LogLevel.Verbose } },
			{ BasicSettings.SupplyUserInput,                   new SettingsSpecification { ValueGetter = values => values, StringFormatter = ApplicationExtensions.ToJsonString } }
		};
	}

	public class SettingsCommandLineSpecifications : IEnumExtensionProvider<BasicSettings, ICommandLineOption>
	{
		public IReadOnlyDictionary<BasicSettings, ICommandLineOption> Specifications { get; } = new Dictionary<BasicSettings, ICommandLineOption>
		{
			{ BasicSettings.ClientInfoPath,                  new SingleValueOption { Flag = "-c|--clientInfoPath <CLIENT_INFO_PATH>", Desc = "The path for the file with the client id and secret for Spotify access" } },
			{ BasicSettings.TokenPath,                       new SingleValueOption { Flag = "-t|--tokenPath <TOKEN_PATH>", Desc = "The path for the file with access and refresh tokens for Spotify access" } },
			{ BasicSettings.RedirectUri,                     new SingleValueOption { Flag = "-r|--redirectUri <REDIRECT_URI>", Desc = "The path Spotify should use as a redirect Uri" } },
			{ BasicSettings.DefaultToAlbumShuffle,           new NoValueOption     { Flag = "--defaultToAlbumShuffle", Desc = "Provide if shuffling the album should be used as a fallback" } },
			{ BasicSettings.ArtistAlbumIncludeGroups,        new SingleValueOption { Flag = "-a|--artistAlbumIncludeGroups", Desc = "The types of albums to include when querying for artists' albums" } },
			{ BasicSettings.TrackQueueSizeLimit,             new SingleValueOption { Flag = "-q|--queueSizeLimit", Desc = "The cap on the number of tracks to send in a request to create a new queue" } },
			{ BasicSettings.MaintainCurrentlyPlaying,        new NoValueOption     { Flag = "--maintainCurrentlyPlaying", Desc = "Provide if playing from the current context should keep what's currently playing" } },
			{ BasicSettings.ConsoleLogLevel,                 new SingleValueOption { Flag = "-cl|--consoleLogLevel", Desc = "The lowest logging level to output to the console. A good default value to provide is \"Info\"" } },
			{ BasicSettings.OutputFileLogLevel,              new SingleValueOption { Flag = "-fl|--fileLogLevel", Desc = "The lowest logging level to output to a file. A good default value to provide is \"Verbose\"" } },
			{ BasicSettings.LogFileName,                     new SingleValueOption { Flag = "--logFileName", Desc = "The name to give to the log file. This should not include the extension or directories" } },
			{ BasicSettings.AskUser,                         new NoValueOption     { Flag = "--askUser", Desc = "Provide if the user should be asked what context to reorder" } },
			{ BasicSettings.TransformationName,              new SingleValueOption { Flag = "--transformation", Desc = "The name of the transformation to be used." } },
			{ BasicSettings.PaginatorName,                   new SingleValueOption { Flag = "--paginator", Desc = "The name of the paginator to be used." } },
			{ BasicSettings.RetryHandlerName,                new SingleValueOption { Flag = "--retryHandler", Desc = "The name of the retry handler to be used." } },
			{ BasicSettings.HTTPLoggerName,                  new SingleValueOption { Flag = "--httpLogger", Desc = "The name of the http logger to be used." } },
			{ BasicSettings.APIConnectorName,                new SingleValueOption { Flag = "--apiConnector", Desc = "The name of the APIConnector to be used." } },
			{ BasicSettings.HTTPLoggerCharacterLimit,        new SingleValueOption { Flag = "--httpLoggerCharacterLimit", Desc = "The max number of characters to print per line from the http logger." } },
			{ BasicSettings.RandomSeed,                      new SingleValueOption { Flag = "--randomSeed", Desc = "The seed to use for random numbers." } },
			{ BasicSettings.MetadataRecordFile,              new SingleValueOption { Flag = "--metadataRecordFile", Desc = "The location to write input to LukesTrackLinker for unit tests" } },
			{ BasicSettings.PlaybackSetterName,              new SingleValueOption { Flag = "--playbackSetter", Desc = "The name of the playback setter to be used." } },
			{ BasicSettings.SaveAsPlaylistName,              new SingleValueOption { Flag = "--playlistName", Desc = "The name of the playlist to save the playback in." } },
			{ BasicSettings.SupplyUserInput,                 new MultiValueOption  { Flag = "--supplyUserInput", Desc = "For testing purposes. Predetermines user input to the terminal." } },
			{ BasicSettings.SpotifyProjectRootDirectory,     new SingleValueOption { Flag = "--projectRoot", Desc = "The root directory containing all the code for this project. This is probably the same as where git info is stored." } },
			{ BasicSettings.NumHTTPConnections,              new SingleValueOption { Flag = "--numHttpConnections", Desc = "The number of http connections to spotify's api to allow" } }
		};
	}

	public class SettingsSpecification : ISettingsSpecification
	{
		public bool IsRequired { get; set; } = false;
		public object Default { get; set; } = null;
		public Func<IEnumerable<string>, object> ValueGetter { get; set; }
			= rawValues => rawValues.TryGetSingle(out var foundResult) && !string.IsNullOrWhiteSpace(foundResult) ? foundResult : default;
		public Func<object, string> StringFormatter { get; set; } = obj => obj?.ToString();
	}

	public abstract class CommandLineOptionBase : ICommandLineOption
	{
		public string Flag { get; set; }
		public string Desc { get; set; }
		public abstract CommandOptionType Type { get; }
		public abstract IEnumerable<string> GetValues(CommandOption option);
	}

	public class NoValueOption : CommandLineOptionBase
	{
		public override CommandOptionType Type => CommandOptionType.NoValue;

		public override IEnumerable<string> GetValues(CommandOption option)
		{
			return option.HasValue() ? new[] { option.ShortName } : Array.Empty<string>();
		}
	}

	public class SingleValueOption : CommandLineOptionBase
	{
		public override CommandOptionType Type => CommandOptionType.SingleValue;

		public override IEnumerable<string> GetValues(CommandOption option)
		{
			return option.HasValue() ? new[] { option.Value() } : Array.Empty<string>();
		}
	}

	public class MultiValueOption : CommandLineOptionBase
	{
		public override CommandOptionType Type => CommandOptionType.MultipleValue;

		public override IEnumerable<string> GetValues(CommandOption option)
		{
			return option.Values;
		}
	}
}
