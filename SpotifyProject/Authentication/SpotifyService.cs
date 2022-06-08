using System;
using System.Threading;
using System.Threading.Tasks;
using ApplicationResources.ApplicationUtils.Parameters;
using ApplicationResources.Logging;
using ApplicationResources.Setup;
using CustomResources.Utils.Concepts;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;
using SpotifyProject.Configuration;
using SpotifyProject.SpotifyAdditions;

namespace SpotifyProject.Authentication
{
    public interface ISpotifyService
    {
        public SpotifyClient Client { get; }
        public PrivateUser CachedUserInfo { get; }
    }

    public abstract class SpotifyProviderBase : StandardDisposable, ISpotifyService
    {
        protected SpotifyProviderBase(ISpotifyAuthenticator spotifyAuthenticator)
        {
            _spotifyAuthenticator = spotifyAuthenticator;
            if (spotifyAuthenticator != null)
            {
                spotifyAuthenticator.OnLoggedIn += OnLoggedIn;
                if (spotifyAuthenticator.IsLoggedIn)
                    _ = OnLoggedIn(spotifyAuthenticator.CurrentAuthenticator, default);
            }
        }

        protected SpotifyProviderBase(ISpotifyAccountAuthenticator spotifyAuthenticator) : this(spotifyAuthenticator.As<ISpotifyAuthenticator>())
        {
            _spotifyAccountAuthenticator = spotifyAuthenticator;
            if (spotifyAuthenticator != null)
                spotifyAuthenticator.OnLoggedOut += OnLoggedOut;
        }

        protected override void DoDispose()
        {
            if (_spotifyAuthenticator != null)
            {
                _spotifyAuthenticator.OnLoggedIn -= OnLoggedIn;
                _spotifyAuthenticator = null;
            }
            if (_spotifyAccountAuthenticator != null)
            {
                _spotifyAccountAuthenticator.OnLoggedOut -= OnLoggedOut;
                _spotifyAccountAuthenticator = null;
            }
        }


        private async Task OnLoggedIn(IAuthenticator authenticator, CancellationToken cancellationToken)
        {
            try
            {
                var config = BuildConfig().WithAuthenticator(authenticator);
                Client = new SpotifyClient(config);
                CachedUserInfo = await Client.UserProfile.Current().WithoutContextCapture();
                Logger.Information("Logged into Spotify account with username: {username}", CachedUsername);
            }
            catch (Exception e)
            {
                Logger.Error("An exception occurred when building the spotify configuration and getting user information: {exception}", e);
                throw;
            }
        }

        private Task OnLoggedOut(CancellationToken _)
        {
            Client = null;
            CachedUserInfo = null;
            return Task.CompletedTask;
        }

        public SpotifyClient Client { get; private set; }

        public PrivateUser CachedUserInfo { get; private set; }
        public string CachedUsername => CachedUserInfo?.DisplayName;

        public abstract SpotifyClientConfig BuildConfig();

        private ISpotifyAuthenticator _spotifyAuthenticator;
        private ISpotifyAccountAuthenticator _spotifyAccountAuthenticator;
    }

    public class StandardSpotifyProvider : SpotifyProviderBase
    {
        public StandardSpotifyProvider(ISpotifyAuthenticator spotifyAuthenticator) : base(spotifyAuthenticator)
        { }

        public StandardSpotifyProvider(ISpotifyAccountAuthenticator spotifyAuthenticator) : base(spotifyAuthenticator)
        { }

        public override SpotifyClientConfig BuildConfig()
        {
            var httpLoggerName = Settings.Get<string>(SpotifySettings.HTTPLoggerName);
            var httpLoggerCharLimit = TaskParameters.Get<int?>(SpotifyParameters.HTTPLoggerCharacterLimit);
            var retryHandlerName = TaskParameters.Get<string>(SpotifyParameters.RetryHandlerName);
            var httpClientName = TaskParameters.Get<string>(SpotifyParameters.HTTPClientName);
            var paginatorName = TaskParameters.Get<string>(SpotifyParameters.PaginatorName);
            var apiConnectorName = TaskParameters.Get<string>(SpotifyParameters.APIConnectorName);
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
            return SpotifyClientConfig.CreateDefault()
                .WithHTTPLogger(httpLogger)
                .WithRetryHandler(retryHandler)
                .WithHTTPClient(httpClientConstructor(retryHandler))
                .WithDefaultPaginator(paginator)
                .WithAPIConnectorConstructor(apiConnectorConstructor);
        }
    }
}

