using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
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

namespace SpotifyProject.Authentication
{
    /**
     * Abstracts out the authentication process
     */
    public abstract class Authenticator
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

        public static async Task<ClientInfo> ReadClientInfoPath(string clientInfoPath)
        {
            Logger.Verbose($"Reading Client Id and Secret from {clientInfoPath}");
            return JsonConvert.DeserializeObject<ClientInfo>(await File.ReadAllTextAsync(clientInfoPath).WithoutContextCapture());
        }
    }

    public static class Authenticators
	{
        public delegate Authenticator AuthenticatorConstructor(SpotifyClientConfig config, string tokenFilePath);

        public static readonly AuthenticatorConstructor AuthorizationCodeAuthenticator
            = (config, tokenFilePath) => new AuthorizationCodeAuthenticator(config, tokenFilePath);

        public static readonly AuthenticatorConstructor ClientCredentialsAuthenticator
            = (config, _) => new ClientCredentialsAuthenticator(config);

        public static async Task<SpotifyClient> Authenticate(AuthenticatorConstructor authenticatorConstructor)
		{
            var projectRoot = Settings.Get<string>(BasicSettings.ProjectRootDirectory);
            var tokenFilePath = Path.Combine(projectRoot, TaskParameters.Get<string>(SpotifyParameters.TokenPath));
            var clientInfoFilePath = Path.Combine(projectRoot, TaskParameters.Get<string>(SpotifyParameters.ClientInfoPath));
            var redirectUri = TaskParameters.Get<string>(SpotifyParameters.RedirectUri);
            var httpLoggerName = Settings.Get<string>(SpotifySettings.HTTPLoggerName);
            var httpLoggerCharLimit = Settings.Get<int?>(SpotifySettings.HTTPLoggerCharacterLimit);
            var retryHandlerName = TaskParameters.Get<string>(SpotifyParameters.RetryHandlerName);
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
            var paginator = SpotifyDefaults.Paginators.TryGetPropertyByName<IPaginator>(paginatorName, out var foundPaginator)
                ? foundPaginator
                : SpotifyDefaults.Paginators.ConcurrentPaginator;
            var apiConnectorConstructor = SpotifyDefaults.APIConnectors.TryGetPropertyByName<APIConnectorConstructor>(apiConnectorName, out var foundConstructor)
                ? foundConstructor
                : SpotifyDefaults.APIConnectors.ModifiedAPIConnector;
            var config = SpotifyClientConfig.CreateDefault()
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
