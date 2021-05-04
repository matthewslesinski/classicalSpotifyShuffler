using System;
using System.Threading.Tasks;
using System.Threading;
using McMaster.Extensions.CommandLineUtils;
using SpotifyAPI.Web;
using SpotifyProject.Authentication;
using SpotifyProject.Utils;
using SpotifyProject.Setup;
using SpotifyProject.SpotifyAdditions;

namespace SpotifyProject
{
    class Program
    {
        private static string _tokenFilePath;
        private static string _redirectUri;
        private static string _clientInfoPath;
        private static bool _suppressAuthenticationLogging;
        private static bool _defaultToAlbumShuffle;

        static void Main(string[] args)
        {

			AppDomain.CurrentDomain.UnhandledException += (sender, args) => Logger.Error($"An Exception occurred: {args.ExceptionObject}");
			var app = new CommandLineApplication();
			var commandLineOptions = CommandLineOptions.AddCommandLineOptions(app);
			app.OnExecute(() =>
			{
				commandLineOptions.ThrowIfMissingRequiredOptions();
				_tokenFilePath = commandLineOptions.GetOptionValue<string>(CommandLineOptions.Names.TokenPath);
				_redirectUri = commandLineOptions.GetOptionValue<string>(CommandLineOptions.Names.RedirectUri);
				_clientInfoPath = commandLineOptions.GetOptionValue<string>(CommandLineOptions.Names.ClientInfoPath);
				_suppressAuthenticationLogging = commandLineOptions.GetOptionValue<bool>(CommandLineOptions.Names.SuppressAuthenticationLogging);
				_defaultToAlbumShuffle = commandLineOptions.GetOptionValue<bool>(CommandLineOptions.Names.DefaultToAlbumShuffle);
				var task = Run();
				while (!task.IsCompleted)
				{
					Thread.Sleep(Timeout.Infinite);
				}
				// Unreachable on purpose in case the compiler would want to get rid of the preceding while loop
				Console.WriteLine("Terminating successfully");
				Environment.Exit(0);
			});
			app.Execute(args);

		}

        static async Task<SpotifyClient> AuthenticateToSpotify()
		{
            if (_suppressAuthenticationLogging)
                Logger.TemporarilySuppressLogging = true;
            Logger.Information("Starting Spotify authentication process");
            var config = SpotifyClientConfig.CreateDefault()
                .WithHTTPLogger(new HTTPLogger())
                .WithRetryHandler(new SimpleRetryHandler())
                .WithDefaultPaginator(new ConcurrentPaginatorWithObservables(new SimplePaginator()));
            var authenticator = new Authentication.AuthorizationCodeAuthenticator(config, _tokenFilePath);
            var clientInfo = await Authenticator.ReadClientInfoPath(_clientInfoPath);
            var authorizationSource = new AuthorizationSource
            {
                ClientInfo = clientInfo,
                RedirectUriString = _redirectUri,
                Scopes = SpotifyConstants.AllAuthenticationScopes
            };
            var spotify = await authenticator.Authenticate(authorizationSource);
            if (_suppressAuthenticationLogging)
                Logger.TemporarilySuppressLogging = false;
            Logger.Information("Successfully logged into Spotify account.");
            return spotify;
        }

        static async Task Run()
        {
            try
            {
                Logger.Information("Starting Spotify Project");
				var spotify = await AuthenticateToSpotify();
				var reorderer = new SpotifyPlaybackReorderer(spotify, _defaultToAlbumShuffle);
                if (GlobalCommandLine.Store.GetOptionValue<bool>(CommandLineOptions.Names.AskUser))
                    await reorderer.ShuffleUserProvidedContext();
                else
				    await reorderer.ShuffleCurrentPlayback();
				Logger.Information("Terminating successfully");
                Environment.Exit(0);
            }
            catch (Exception e)
            {
                Logger.Error($"An Exception occurred: {e}");
                Logger.Information("Terminating due to error");
                Environment.Exit(1);
            }
        }
    }
}
