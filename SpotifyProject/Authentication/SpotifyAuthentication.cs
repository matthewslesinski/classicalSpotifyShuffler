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
using System.Threading;

namespace SpotifyProject.Authentication
{
    public interface ISpotifyAuthenticator : IOAuthService<AuthorizationSource, IAuthenticator>
    {
        IAuthenticator CurrentAuthenticator { get; }
        bool IsLoggedIn { get; }

        Task<Result<IAuthenticator>> TryImmediateLogIn(CancellationToken cancellationToken = default);

        public Task<Result<IAuthenticator>> LogIn(ClientInfo userInfo, CancellationToken cancellationToken = default)
        {
            var redirectUri = TaskParameters.Get<string>(SpotifyParameters.RedirectUri);
            var authorizationSource = new AuthorizationSource
            {
                ClientInfo = userInfo,
                RedirectUriString = redirectUri,
                Scopes = SpotifyConstants.AllAuthenticationScopes,
            };
            return this.As<IAuthenticationService<AuthorizationSource, IAuthenticator>>().LogIn(authorizationSource, cancellationToken);
        }
    }

    public interface ISpotifyAccountAuthenticator : IAuthCodeAuthenticationService<AuthorizationSource, IAuthenticator>, ISpotifyAuthenticator { }

    public static class SpotifyAuthentication
	{
        public static async Task<IAuthenticator> Authenticate(this ISpotifyAuthenticator spotifyAuthenticator, CancellationToken cancellationToken = default)
		{
            Logger.Information("Starting Spotify authentication process");
            var projectRoot = Settings.Get<string>(BasicSettings.ProjectRootDirectory);
            var personalDataDirectory = Settings.Get<string>(SpotifySettings.PersonalDataDirectory);
            var clientInfoFilePath = GeneralUtils.GetAbsoluteCombinedPath(projectRoot, personalDataDirectory, TaskParameters.Get<string>(SpotifyParameters.ClientInfoPath));
            var clientInfo = await ReadClientInfoPath(clientInfoFilePath).WithoutContextCapture();
            var authenticator = await spotifyAuthenticator.LogIn(clientInfo, cancellationToken);
            if (!authenticator.Success)
                throw new Exception($"Spotify Login attempt failed");
            Logger.Information("Successfully logged into Spotify account.");
            return authenticator.ResultValue;
        }

        private static async Task<ClientInfo> ReadClientInfoPath(string clientInfoPath)
        {
            Logger.Verbose($"Reading Client Id and Secret from {clientInfoPath}");
            var (foundClientInfo, clientInfo) = await GlobalDependencies.GlobalDependencyContainer.GetLocalDataStore().TryGetAsync(clientInfoPath, CachePolicy.PreferActual).WithoutContextCapture();
            if (!foundClientInfo)
                throw new KeyNotFoundException("Cannot find the client ID and secret necessary for authenticating with the Spotify API");
            return clientInfo.FromJsonString<ClientInfo>();
        }
	}
}
