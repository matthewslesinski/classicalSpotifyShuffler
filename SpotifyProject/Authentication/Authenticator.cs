using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;
using ApplicationResources.Setup;
using SpotifyProject.SpotifyAdditions;
using SpotifyProject.Utils;
using CustomResources.Utils.GeneralUtils;
using CustomResources.Utils.Extensions;
using ApplicationResources.Logging;
using SpotifyProject.Configuration;
using ApplicationResources.ApplicationUtils.Parameters;
using ApplicationResources.Utils;
using System.Collections.Generic;
using ApplicationResources.Services;
using CustomResources.Utils.Concepts;

namespace SpotifyProject.Authentication
{
    /**
     * Abstracts out the authentication process
     */
    public abstract class Authenticator : IGlobalServiceUser
    {
        protected SpotifyClientConfig _config;

        public Authenticator(SpotifyClientConfig config)
        {
            _config = config;
        }

        protected abstract Task<IAuthenticator> GetAuthenticator(AuthorizationSource authorizationSource);

		public async Task<SpotifyClient> Authenticate(AuthorizationSource authorizationSource)
		{
            var authenticator = await GetAuthenticator(authorizationSource).WithoutContextCapture();
            _config = _config.WithAuthenticator(authenticator);
            return new SpotifyClient(_config);
		}

        public async Task<ClientInfo> ReadClientInfoPath(string clientInfoPath)
        {
            Logger.Verbose($"Reading Client Id and Secret from {clientInfoPath}");
            var (foundClientInfo, clientInfo) = await ReadStoredData(clientInfoPath).WithoutContextCapture();
            if (!foundClientInfo)
                throw new KeyNotFoundException("Cannot find the client ID and secret necessary for authenticating with the Spotify API");
            return clientInfo.FromJsonString<ClientInfo>();
        }

        protected Task<Result<string>> ReadStoredData(string storedDataPath) => this.AccessLocalDataStore().TryGetAsync(storedDataPath);
    }

    public static class Authenticators
	{
        public delegate Authenticator AuthenticatorConstructor(SpotifyClientConfig config, string tokenFilePath);

        public static readonly AuthenticatorConstructor AuthorizationCodeAuthenticator
            = (config, tokenFilePath) => new AuthorizationCodeCommandLineAuthenticator(config, tokenFilePath);

        public static readonly AuthenticatorConstructor ClientCredentialsAuthenticator
            = (config, _) => new ClientCredentialsAuthenticator(config);

        public static async Task<SpotifyClient> Authenticate(AuthenticatorConstructor authenticatorConstructor)
		{
            var projectRoot = Settings.Get<string>(BasicSettings.ProjectRootDirectory);
            var tokenFilePath = Path.Combine(projectRoot, TaskParameters.Get<string>(SpotifyParameters.TokenPath));
            var clientInfoFilePath = Path.Combine(projectRoot, TaskParameters.Get<string>(SpotifyParameters.ClientInfoPath));
            var redirectUri = TaskParameters.Get<string>(SpotifyParameters.RedirectUri);
            var httpLoggerName = Settings.Get<string>(SpotifySettings.HTTPLoggerName);
            var httpLoggerCharLimit = TaskParameters.Get<int?>(SpotifyParameters.HTTPLoggerCharacterLimit);
            var retryHandlerName = TaskParameters.Get<string>(SpotifyParameters.RetryHandlerName);
            var httpClientName = TaskParameters.Get<string>(SpotifyParameters.HTTPClientName);
            var paginatorName = TaskParameters.Get<string>(SpotifyParameters.PaginatorName);
            var apiConnectorName = TaskParameters.Get<string>(SpotifyParameters.APIConnectorName);
            Logger.Information("Starting Spotify authentication process");
            ServicePointManager.DefaultConnectionLimit = Settings.Get<int>(SpotifySettings.NumHTTPConnections);
            var httpLogger = SpotifyDefaults.HTTPLoggers.TryGetPropertyByName<ITruncatedHTTPLogger>(httpLoggerName, out var foundLogger)
                ? foundLogger
                : SpotifyDefaults.HTTPLoggers.InternalLoggingWrapper;
            httpLogger.CharacterLimit = httpLoggerCharLimit;
            var retryHandler = SpotifyDefaults.RetryHandlers.TryGetPropertyByName<IRetryHandler>(retryHandlerName, out var foundRetryHandler)
                ? foundRetryHandler
                : SpotifyDefaults.RetryHandlers.SimpleRetryHandler;
            var httpClientConstructor = SpotifyDefaults.HTTPClients.TryGetPropertyByName<HttpClients.HttpClientConstructor>(httpClientName, out var foundHttpClientConstructor)
                ? foundHttpClientConstructor
                : SpotifyDefaults.HTTPClients.NetHttpClient;
            var paginator = SpotifyDefaults.Paginators.TryGetPropertyByName<IPaginator>(paginatorName, out var foundPaginator)
                ? foundPaginator
                : SpotifyDefaults.Paginators.ConcurrentPaginator;
            var apiConnectorConstructor = SpotifyDefaults.APIConnectors.TryGetPropertyByName<APIConnectorConstructor>(apiConnectorName, out var foundApiConnectorConstructor)
                ? foundApiConnectorConstructor
                : SpotifyDefaults.APIConnectors.ModifiedAPIConnector;
            var config = SpotifyClientConfig.CreateDefault()
                .WithHTTPLogger(httpLogger)
                .WithRetryHandler(retryHandler)
                .WithHTTPClient(httpClientConstructor(retryHandler))
                .WithDefaultPaginator(paginator)
                .WithAPIConnectorConstructor(apiConnectorConstructor);
            var authenticator = authenticatorConstructor(config, tokenFilePath);
            var clientInfo = await authenticator.ReadClientInfoPath(clientInfoFilePath).WithoutContextCapture();
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
