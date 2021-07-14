using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;
using SpotifyProject.Setup;
using SpotifyProject.SpotifyAdditions;
using SpotifyProject.Utils;

namespace SpotifyProject.Authentication
{
    /**
     * Abstracts out the authentication process
     */
    public abstract class Authenticator : SpotifyCommunicator
    {
        public Authenticator(SpotifyClientConfig config) : base(config)
        {
        }

        protected abstract Task<IAuthenticator> GetAuthenticator(AuthorizationSource authorizationSource);

		public async Task<SpotifyClient> Authenticate(AuthorizationSource authorizationSource)
		{
            var authenticator = await GetAuthenticator(authorizationSource).WithoutContextCapture();
            return new SpotifyClient(_config.WithAuthenticator(authenticator));
		}

        public static async Task<ClientInfo> ReadClientInfoPath(string clientInfoPath)
        {
            Logger.Verbose($"Reading Client Id and Secret from {clientInfoPath}");
            return JsonConvert.DeserializeObject<ClientInfo>(await File.ReadAllTextAsync(clientInfoPath).WithoutContextCapture());
        }
    }

    public static class Authenticators
	{
        public static readonly Func<SpotifyClientConfig, string, Authenticator> AuthorizationCodeAuthenticator
            = (config, tokenFilePath) => new AuthorizationCodeAuthenticator(config, tokenFilePath);

        public static readonly Func<SpotifyClientConfig, string, Authenticator> ClientCredentialsAuthenticator
            = (config, _) => new ClientCredentialsAuthenticator(config);

        public static async Task<SpotifyClient> Authenticate(Func<SpotifyClientConfig, string, Authenticator> authenticatorConstructor)
		{
            var tokenFilePath = Settings.Get<string>(SettingsName.TokenPath);
            var clientInfoFilePath = Settings.Get<string>(SettingsName.ClientInfoPath);
            var redirectUri = Settings.Get<string>(SettingsName.RedirectUri);
            var httpLoggerName = Settings.Get<string>(SettingsName.HTTPLoggerName);
            var httpLoggerCharLimit = Settings.Get<int?>(SettingsName.HTTPLoggerCharacterLimit);
            var retryHandlerName = Settings.Get<string>(SettingsName.RetryHandlerName);
            var paginatorName = Settings.Get<string>(SettingsName.PaginatorName);
            Logger.Information("Starting Spotify authentication process");
            ServicePointManager.DefaultConnectionLimit = Settings.Get<int>(SettingsName.NumHTTPConnections);
            var httpLogger = SpotifyDefaults.HTTPLoggers.TryGetPropertyByName<ITruncatedHTTPLogger>(httpLoggerName, out var foundLogger)
                ? foundLogger
                : SpotifyDefaults.HTTPLoggers.InternalLoggingWrapper;
            httpLogger.CharacterLimit = httpLoggerCharLimit;
            var retryHandler = SpotifyDefaults.RetryHandlers.TryGetPropertyByName<IRetryHandler>(retryHandlerName, out var foundRetryHandler)
                ? foundRetryHandler
                : SpotifyDefaults.RetryHandlers.SimpleRetryHandler;
            var paginator = SpotifyDefaults.Paginators.TryGetPropertyByName<IPaginator>(paginatorName, out var foundPaginator)
                ? foundPaginator
                : SpotifyDefaults.Paginators.ConcurrentObservablePaginator;
            var config = SpotifyClientConfig.CreateDefault()
                .WithHTTPLogger(httpLogger)
                .WithRetryHandler(retryHandler)
                .WithDefaultPaginator(paginator);
            var authenticator = authenticatorConstructor(config, tokenFilePath);
            var clientInfo = await Authenticator.ReadClientInfoPath(clientInfoFilePath).WithoutContextCapture();
            var authorizationSource = new AuthorizationSource
            {
                ClientInfo = clientInfo,
                RedirectUriString = redirectUri,
                Scopes = SpotifyConstants.AllAuthenticationScopes
            };
            var spotify = await authenticator.Authenticate(authorizationSource).WithoutContextCapture();
            Logger.Information("Successfully logged into Spotify account.");
            return spotify;
        }
    }
}
