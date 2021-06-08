using System;
using System.Collections.Generic;
using McMaster.Extensions.CommandLineUtils;
using System.Linq;

namespace SpotifyProject.Setup
{
	public static class GlobalCommandLine
	{
		public static Dictionary<string, CommandOption> Store;
	}

	public static class CommandLineOptions
	{

		public static class Names
		{
			public const string ClientInfoPath = nameof(ClientInfoPath);
			public const string TokenPath = nameof(TokenPath);
			public const string RedirectUri = nameof(RedirectUri);
			public const string SuppressAuthenticationLogging = nameof(SuppressAuthenticationLogging);
			public const string DefaultToAlbumShuffle = nameof(DefaultToAlbumShuffle);
			public const string ArtistAlbumIncludeGroups = nameof(ArtistAlbumIncludeGroups);
			public const string TrackQueueSizeLimit = nameof(TrackQueueSizeLimit);
			public const string MaintainCurrentlyPlaying = nameof(MaintainCurrentlyPlaying);
			public const string LogLevel = nameof(LogLevel);
			public const string AskUser = nameof(AskUser);
			public const string TransformationName = nameof(TransformationName);
			public const string RandomSeed = nameof(RandomSeed);
			public const string MetadataRecordFile = nameof(MetadataRecordFile);
			public const string PlaybackSetterName = nameof(PlaybackSetterName);
			public const string SupplyUserInput = nameof(SupplyUserInput);
		}

		private readonly static Dictionary<string, CommandLineOption> _config = new Dictionary<string, CommandLineOption>
		{
			{ Names.ClientInfoPath, new CommandLineOption{Flag = "-c|--clientInfoPath <CLIENT_INFO_PATH>", Desc = "The path for the file with the client id and secret for Spotify access", IsRequired = true} },
			{ Names.TokenPath, new CommandLineOption{Flag = "-t|--tokenPath <TOKEN_PATH>", Desc = "The path for the file with access and refresh tokens for Spotify access" } },
			{ Names.RedirectUri, new CommandLineOption{Flag = "-r|--redirectUri <REDIRECT_URI>", Desc = "The path Spotify should use as a redirect Uri", IsRequired = true } },
			{ Names.SuppressAuthenticationLogging, new CommandLineOption{Flag = "--suppressAuthenticationLogging", Desc = "Provide if logging should be suppressed during authentication", Type = CommandOptionType.NoValue, ValueGetter = option => option.HasValue() } },
			{ Names.DefaultToAlbumShuffle, new CommandLineOption{Flag = "--defaultToAlbumShuffle", Desc = "Provide if shuffling the album should be used as a fallback", Type = CommandOptionType.NoValue, ValueGetter = option => option.HasValue() } },
			{ Names.ArtistAlbumIncludeGroups, new CommandLineOption{Flag = "-a|--artistAlbumIncludeGroups", Desc = "The types of albums to include when querying for artists' albums", Type = CommandOptionType.SingleValue, ValueGetter = option => option.Value().Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() } },
			{ Names.TrackQueueSizeLimit, new CommandLineOption{Flag = "-q|--queueSizeLimit", Desc = "The cap on the number of tracks to send in a request to create a new queue", Type = CommandOptionType.SingleValue, ValueGetter = option => option.HasValue() ? int.Parse(option.Value()) : 750} },
			{ Names.MaintainCurrentlyPlaying, new CommandLineOption{Flag = "--maintainCurrentlyPlaying", Desc = "Provide if playing from the current context should keep what's currently playing", Type = CommandOptionType.NoValue, ValueGetter = option => option.HasValue() } },
			{ Names.LogLevel, new CommandLineOption{Flag = "-l|--logLevel", Desc = "The lowest logging level to output. A good default value to provide is \"Info\"", Type = CommandOptionType.SingleValue, ValueGetter = option => Enum.Parse<LogLevel>(option.Value(), true), IsRequired = true } },
			{ Names.AskUser, new CommandLineOption{Flag = "--askUser", Desc = "Provide if the user should be asked what context to reorder", Type = CommandOptionType.NoValue, ValueGetter = option => option.HasValue() } },
			{ Names.TransformationName, new CommandLineOption{Flag = "--transformation", Desc = "The name of the transformation to be used."} },
			{ Names.RandomSeed, new CommandLineOption{Flag = "--randomSeed", Desc = "The seed to use for random numbers.", ValueGetter = option => option.HasValue() ? int.Parse(option.Value()) : (int?) null } },
			{ Names.MetadataRecordFile, new CommandLineOption{Flag = "--metadataRecordFile", Desc = "The location to write input to LukesTrackLinker for unit tests" } },
			{ Names.PlaybackSetterName, new CommandLineOption{Flag = "--playbackSetter", Desc = "The name of the playback setter to be used."} },
			{ Names.SupplyUserInput, new CommandLineOption{Flag = "--supplyUserInput", Desc = "For testing purposes. Predetermines user input to the terminal.", Type = CommandOptionType.MultipleValue, ValueGetter = option => option.Values } }
		};

		public static Dictionary<string, CommandOption> AddCommandLineOptions(CommandLineApplication app, bool makeGlobal = true)
		{
			app.HelpOption();
			var result = _config.ToDictionary(kvp => kvp.Key, kvp => app.Option(kvp.Value.Flag, kvp.Value.Desc, kvp.Value.Type));
			if (makeGlobal)
				GlobalCommandLine.Store = result;
			return result;
		}

		public static void ThrowIfMissingRequiredOptions(this Dictionary<string, CommandOption> options)
		{
			var exceptions = options.Where(kvp => _config[kvp.Key].IsRequired && !kvp.Value.HasValue())
				.Select(kvp => new KeyNotFoundException($"A value for command line argument {kvp.Key} is required but nothing was provided")).ToList();
			if (exceptions.Any())
			{
				var exception = exceptions.Count() > 1 ? (Exception)new AggregateException(exceptions) : exceptions.First();
				throw exception;
			}
		}

		public static T GetOptionValue<T>(this Dictionary<string, CommandOption> options, string optionName) {
			try
			{
				return (T)_config[optionName].ValueGetter(options[optionName]);
			}
			catch (Exception e)
			{
				Console.Error.WriteLine($"There was a problem retrieving the command line argument for {optionName}: {e}");
				return default;
			}
		}


		private class CommandLineOption
		{
			internal string Flag { get; set; }
			internal string Desc { get; set; }
			internal CommandOptionType Type { get; set; } = CommandOptionType.SingleValue;
			internal bool IsRequired { get; set; } = false;
			internal Func<CommandOption, object> ValueGetter { get; set; } = option => option.HasValue() ?  option.Value() : default;
		}
	}
}
