using System;
using System.Threading;
using System.Threading.Tasks;
using ApplicationResources.ApplicationUtils.Parameters;
using ApplicationResources.Logging;
using ApplicationResources.Setup;
using CustomResources.Utils.Concepts;
using CustomResources.Utils.Concepts.DataStructures;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;
using SpotifyProject.Configuration;
using SpotifyProject.SpotifyAdditions;
using Util = CustomResources.Utils.GeneralUtils.Utils;

namespace SpotifyProject.Authentication
{
    public interface ISpotifyService
    {
        SpotifyClient Client { get; }
        PrivateUser CachedUserInfo { get; }
        Task InitializeAsync(CancellationToken cancellationToken = default);
    }

    public abstract class SpotifyProviderBase : StandardDisposable, ISpotifyService
    {
        protected SpotifyProviderBase(ISpotifyAuthenticator spotifyAuthenticator)
        {
            _spotifyAuthenticator = spotifyAuthenticator;
        }

        public Task InitializeAsync(CancellationToken cancellationToken = default)
		{
            if (_alreadyDisposed.AsBool())
                throw new InvalidOperationException("Can't initialized when already disposed");
            return Util.LoadOnceBlockingAsync(_isLoaded, _lock, async (cancellationToken) =>
            {
                if (_spotifyAuthenticator != null)
                {
                    _spotifyAuthenticator.OnLoggedIn += OnLoggedIn;
                    if (_spotifyAuthenticator.IsLoggedIn)
                        await OnLoggedIn(_spotifyAuthenticator.CurrentAuthenticator, cancellationToken).WithoutContextCapture();
                }
            }, cancellationToken);
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
                using (await _lock.AcquireToken(cancellationToken).WithoutContextCapture())
                {
                    var config = BuildConfig().WithAuthenticator(authenticator);
                    Client = new SpotifyClient(config);
                    CachedUserInfo = await Client.UserProfile.Current().WithoutContextCapture();
                    Logger.Information("Logged into Spotify account with username: {username}", CachedUsername);
                }
            }
            catch (Exception e)
            {
                Logger.Error("An exception occurred when building the spotify configuration and getting user information: {exception}", e);
                throw;
            }
        }

        private async Task OnLoggedOut(CancellationToken _)
        {
            using (await _lock.AcquireToken(_).WithoutContextCapture())
            {
                Client = null;
                CachedUserInfo = null;
            }
        }

        public SpotifyClient Client { get; private set; }

        public PrivateUser CachedUserInfo { get; private set; }
        public string CachedUsername => CachedUserInfo?.DisplayName;

        public abstract SpotifyClientConfig BuildConfig();

        private ISpotifyAuthenticator _spotifyAuthenticator;
        private ISpotifyAccountAuthenticator _spotifyAccountAuthenticator;
        private readonly MutableReference<bool> _isLoaded = new(false);
        private readonly AsyncLockProvider _lock = new();
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

