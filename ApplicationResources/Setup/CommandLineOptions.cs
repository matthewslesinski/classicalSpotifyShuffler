using System;
using System.Collections.Generic;
using McMaster.Extensions.CommandLineUtils;
using System.Linq;

namespace ApplicationResources.Setup
{
	public class CommandLineOptions : ISettingsProvider<SettingsName>
	{
		private readonly Dictionary<SettingsName, CommandOption> _options;
		private CommandLineOptions(Dictionary<SettingsName, CommandOption> options) => _options = options;

		private readonly static Dictionary<SettingsName, ICommandLineOption> _config = new Dictionary<SettingsName, ICommandLineOption>
		{
			{ SettingsName.ClientInfoPath,					new SingleValueOption { Flag = "-c|--clientInfoPath <CLIENT_INFO_PATH>", Desc = "The path for the file with the client id and secret for Spotify access" } },
			{ SettingsName.TokenPath,						new SingleValueOption { Flag = "-t|--tokenPath <TOKEN_PATH>", Desc = "The path for the file with access and refresh tokens for Spotify access" } },
			{ SettingsName.RedirectUri,						new SingleValueOption { Flag = "-r|--redirectUri <REDIRECT_URI>", Desc = "The path Spotify should use as a redirect Uri" } },
			{ SettingsName.DefaultToAlbumShuffle,			new NoValueOption	  { Flag = "--defaultToAlbumShuffle", Desc = "Provide if shuffling the album should be used as a fallback" } },
			{ SettingsName.ArtistAlbumIncludeGroups,		new SingleValueOption { Flag = "-a|--artistAlbumIncludeGroups", Desc = "The types of albums to include when querying for artists' albums" } },
			{ SettingsName.TrackQueueSizeLimit,				new SingleValueOption { Flag = "-q|--queueSizeLimit", Desc = "The cap on the number of tracks to send in a request to create a new queue" } },
			{ SettingsName.MaintainCurrentlyPlaying,		new NoValueOption	  { Flag = "--maintainCurrentlyPlaying", Desc = "Provide if playing from the current context should keep what's currently playing" } },
			{ SettingsName.ConsoleLogLevel,                 new SingleValueOption { Flag = "-cl|--consoleLogLevel", Desc = "The lowest logging level to output to the console. A good default value to provide is \"Info\"" } },
			{ SettingsName.OutputFileLogLevel,              new SingleValueOption { Flag = "-fl|--fileLogLevel", Desc = "The lowest logging level to output to a file. A good default value to provide is \"Verbose\"" } },
			{ SettingsName.LogFileName,						new SingleValueOption { Flag = "--logFileName", Desc = "The name to give to the log file. This should not include the extension or directories" } },
			{ SettingsName.AskUser,							new NoValueOption	  { Flag = "--askUser", Desc = "Provide if the user should be asked what context to reorder" } },
			{ SettingsName.TransformationName,              new SingleValueOption { Flag = "--transformation", Desc = "The name of the transformation to be used." } },
			{ SettingsName.PaginatorName,			        new SingleValueOption { Flag = "--paginator", Desc = "The name of the paginator to be used." } },
			{ SettingsName.RetryHandlerName,				new SingleValueOption { Flag = "--retryHandler", Desc = "The name of the retry handler to be used." } },
			{ SettingsName.HTTPLoggerName,                  new SingleValueOption { Flag = "--httpLogger", Desc = "The name of the http logger to be used." } },
			{ SettingsName.APIConnectorName,                new SingleValueOption { Flag = "--apiConnector", Desc = "The name of the APIConnector to be used." } },
			{ SettingsName.HTTPLoggerCharacterLimit,        new SingleValueOption { Flag = "--httpLoggerCharacterLimit", Desc = "The max number of characters to print per line from the http logger." } },
			{ SettingsName.RandomSeed,						new SingleValueOption { Flag = "--randomSeed", Desc = "The seed to use for random numbers." } },
			{ SettingsName.MetadataRecordFile,				new SingleValueOption { Flag = "--metadataRecordFile", Desc = "The location to write input to LukesTrackLinker for unit tests" } },
			{ SettingsName.PlaybackSetterName,				new SingleValueOption { Flag = "--playbackSetter", Desc = "The name of the playback setter to be used." } },
			{ SettingsName.SaveAsPlaylistName,				new SingleValueOption { Flag = "--playlistName", Desc = "The name of the playlist to save the playback in." } },
			{ SettingsName.SupplyUserInput,                 new MultiValueOption  { Flag = "--supplyUserInput", Desc = "For testing purposes. Predetermines user input to the terminal." } },
			{ SettingsName.SpotifyProjectRootDirectory,     new SingleValueOption { Flag = "--projectRoot", Desc = "The root directory containing all the code for this project. This is probably the same as where git info is stored." } },
			{ SettingsName.NumHTTPConnections,				new SingleValueOption { Flag = "--numHttpConnections", Desc = "The number of http connections to spotify's api to allow" } }
		};

		public static ISettingsProvider<SettingsName> Initialize(CommandLineApplication app)
		{
			app.HelpOption();
			var result = _config.ToDictionary(kvp => kvp.Key, kvp => app.Option(kvp.Value.Flag, kvp.Value.Desc, kvp.Value.Type));
			var settingsProvider = new CommandLineOptions(result);
			return settingsProvider;
		}

		public bool IsLoaded => true;

		public void Load()
		{
			// no need
		}

		public bool TryGetValues(SettingsName setting, out IEnumerable<string> values)
		{
			var option = _options[setting];
			if (option.HasValue())
			{
				values = _config[setting].GetValues(option);
				return true;
			}
			values = Array.Empty<string>();
			return false;
		}

		private interface ICommandLineOption
		{
			string Flag { get; set; }
			string Desc { get; set; }
			CommandOptionType Type { get; }
			IEnumerable<string> GetValues(CommandOption option);
		}

		private abstract class CommandLineOptionBase : ICommandLineOption
		{
			public string Flag { get; set; }
			public string Desc { get; set; }
			public abstract CommandOptionType Type { get; }
			public abstract IEnumerable<string> GetValues(CommandOption option);
		}

		private class NoValueOption : CommandLineOptionBase
		{
			public override CommandOptionType Type => CommandOptionType.NoValue;

			public override IEnumerable<string> GetValues(CommandOption option)
			{
				return option.HasValue() ? new[] { option.ShortName } : Array.Empty<string>();
			}
		}

		private class SingleValueOption : CommandLineOptionBase
		{
			public override CommandOptionType Type => CommandOptionType.SingleValue;

			public override IEnumerable<string> GetValues(CommandOption option)
			{
				return option.HasValue() ? new[] { option.Value() } : Array.Empty<string>();
			}
		}

		private class MultiValueOption : CommandLineOptionBase
		{
			public override CommandOptionType Type => CommandOptionType.MultipleValue;

			public override IEnumerable<string> GetValues(CommandOption option)
			{
				return option.Values;
			}
		}
	}
}
