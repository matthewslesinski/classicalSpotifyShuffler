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
using SpotifyProject.Utils.GeneralUtils;
using SpotifyProject.Utils.Extensions;

namespace SpotifyProject.Authentication
{
    /**
     * Abstracts out the authentication process
     */
    public abstract class Authenticator
    {
        protected readonly SpotifyClientConfigHolder _config;

        public Authenticator(SpotifyClientConfigHolder config)
        {
            _config = config;
        }

        protected abstract Task<IAuthenticator> GetAuthenticator(AuthorizationSource authorizationSource);

		public async Task<SpotifyClient> Authenticate(AuthorizationSource authorizationSource)
		{
            var authenticator = await GetAuthenticator(authorizationSource).WithoutContextCapture();
            var authenticatedConfig = _config.WithAuthenticator(authenticator).Finalized();
            return new SpotifyClient(_config.UnderlyingSpotifyClientConfig);
		}

        public static async Task<ClientInfo> ReadClientInfoPath(string clientInfoPath)
        {
            Logger.Verbose($"Reading Client Id and Secret from {clientInfoPath}");
            return JsonConvert.DeserializeObject<ClientInfo>(await File.ReadAllTextAsync(clientInfoPath).WithoutContextCapture());
        }
    }

    public static class Authenticators
	{
        public static readonly Func<SpotifyClientConfigHolder, string, Authenticator> AuthorizationCodeAuthenticator
            = (config, tokenFilePath) => new AuthorizationCodeAuthenticator(config, tokenFilePath);

        public static readonly Func<SpotifyClientConfigHolder, string, Authenticator> ClientCredentialsAuthenticator
            = (config, _) => new ClientCredentialsAuthenticator(config);

        public static async Task<SpotifyClient> Authenticate(Func<SpotifyClientConfigHolder, string, Authenticator> authenticatorConstructor)
		{
            var tokenFilePath = Settings.Get<string>(SettingsName.TokenPath);
            var clientInfoFilePath = Settings.Get<string>(SettingsName.ClientInfoPath);
            var redirectUri = Settings.Get<string>(SettingsName.RedirectUri);
            var httpLoggerName = Settings.Get<string>(SettingsName.HTTPLoggerName);
            var httpLoggerCharLimit = Settings.Get<int?>(SettingsName.HTTPLoggerCharacterLimit);
            var retryHandlerName = Settings.Get<string>(SettingsName.RetryHandlerName);
            var paginatorName = Settings.Get<string>(SettingsName.PaginatorName);
            var apiConnectorName = Settings.Get<string>(SettingsName.APIConnectorName);
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
            var apiConnectorConstructor = SpotifyDefaults.APIConnectors.TryGetPropertyByName<APIConnectors.APIConnectorConstructor>(apiConnectorName, out var foundConstructor)
                ? foundConstructor
                : SpotifyDefaults.APIConnectors.ModifiedAPIConnector;
            var config = SpotifyClientConfigHolder.CreateWithDefaultConfig()
                .WithHTTPLogger(httpLogger)
                .WithRetryHandler(retryHandler)
                .WithDefaultPaginator(paginator)
                .WithAPIConnectorConstructor(apiConnectorConstructor);
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
