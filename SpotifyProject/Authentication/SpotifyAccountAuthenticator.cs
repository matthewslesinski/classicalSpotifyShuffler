using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ApplicationResources.ApplicationUtils.Parameters;
using ApplicationResources.Logging;
using ApplicationResources.Services;
using ApplicationResources.Setup;
using ApplicationResources.Utils;
using CustomResources.Utils.Concepts;
using CustomResources.Utils.Extensions;
using Newtonsoft.Json;
using SpotifyAPI.Web;
using SpotifyProject.Configuration;

namespace SpotifyProject.Authentication
{
	public abstract class SpotifyAccountAuthenticator : ManualAuthorizationCodeAuthenticatorService<AuthorizationSource, AuthorizationCodeTokensWrapper, SessionDefinition, IAuthenticator>, ISpotifyAccountAuthenticator
	{
		protected SpotifyAccountAuthenticator()
		{
			Task ChangeLoggedInState(bool intendedState)
			{
				_isLoggedIn = intendedState;
				return Task.CompletedTask;
			}
			OnLoggedIn += (_, _) => ChangeLoggedInState(true);
			OnLoggedOut += (_) => ChangeLoggedInState(false);

			OnLoggedIn += (result, _) =>
			{
				CurrentAuthenticator = result;
				return Task.CompletedTask;
			};

			OnLoggedOut += (_) =>
			{
				CurrentAuthenticator = null;
				return Task.CompletedTask;
			};
		}

		public bool IsLoggedIn => _isLoggedIn;
		public override Task<bool> GetIsLoggedIn(CancellationToken _ = default)
		{
			return Task.FromResult(_isLoggedIn);
		}

		protected override Uri DetermineLoginAddress(AuthorizationSource userInfo)
		{
			var loginRequest = new LoginRequest(userInfo.RedirectUri, userInfo.ClientId, LoginRequest.ResponseType.Code) { Scope = userInfo.Scopes };
			return loginRequest.ToUri();
		}

		protected override Task<AuthorizationCodeTokensWrapper> AcquireTokens(AuthorizationSource userInfo, string authCode, CancellationToken cancellationToken = default) =>
			new OAuthClient().RequestToken(new AuthorizationCodeTokenRequest(userInfo.ClientId, userInfo.ClientSecret, authCode, userInfo.RedirectUri), cancellationToken)
				.Then(authTokens => new AuthorizationCodeTokensWrapper { TokenResponse = authTokens });

		protected override Task<IAuthenticator> GetAuthenticatedOutputFromTokens(SessionDefinition sessionInfo, CancellationToken _ = default)
		{
			var authenticator = new AuthorizationCodeAuthenticator(sessionInfo.AuthSource.ClientId, sessionInfo.AuthSource.ClientSecret, sessionInfo.AuthTokens);
			if (sessionInfo.SaveKey != null)
				authenticator.TokenRefreshed += (sender, token) => PersistCurrentSession(sessionInfo);
			return Task.FromResult<IAuthenticator>(authenticator);
		}

		protected override Task<Result<SessionDefinition>> GetExistingSession(CancellationToken cancellationToken = default)
		{
			var key = CurrentSessionKey;
			if (key == null)
				return Task.FromResult(Result<SessionDefinition>.NotFound);
			return this.AccessLocalDataStore().TryGetAsync(key, cancellationToken).Then(result => result.Transform(json =>
			{
				Logger.Verbose($"Reading Spotify access token from {key}");
				var sessionInfo = json.FromJsonString<SessionDefinition>();
				sessionInfo.SaveKey = key;
				return sessionInfo;
			}));
		}

		protected override Task PersistCurrentSession(SessionDefinition sessionInfo, CancellationToken cancellationToken = default)
		{
			var key = sessionInfo?.SaveKey ?? CurrentSessionKey;
			if (key == null)
				return Task.CompletedTask;
			
			Logger.Verbose($"Writing new Spotify access/refresh tokens to {key}");
			var json = sessionInfo.ToJsonString();
			return this.AccessLocalDataStore().SaveAsync(key, json, cancellationToken);
		}

		protected override SessionDefinition BuildSessionInfo(AuthorizationSource userInfo, AuthorizationCodeTokensWrapper tokens) =>
			new SessionDefinition(userInfo, tokens) { SaveKey = CurrentSessionKey };

		public IAuthenticator CurrentAuthenticator
		{
			get
			{
				if (!_isLoggedIn)
					throw new InvalidOperationException("Cannot get an authenticator when not logged in");
				return _currentAuthenticator;
			}
			protected set
			{
				_currentAuthenticator = value;
			}
		}
		private IAuthenticator _currentAuthenticator;

		protected bool _isLoggedIn;

		private static string CurrentSessionKey => Settings.TryGet<string>(BasicSettings.ProjectRootDirectory, out var root) && TaskParameters.TryGet<string>(SpotifyParameters.TokenPath, out var tokenPath)
			? Path.Combine(root, tokenPath)
			: null;
	}

	public class SpotifyCommandLineAccountAuthenticator : SpotifyAccountAuthenticator
	{
		protected override async Task<string> RequestLoginFromUser(Uri loginUri, CancellationToken cancellationToken = default)
		{
			var ui = this.AccessUserInterface();
			ui.NotifyUser($"Please go to the following address to login to Spotify: \n\n{loginUri}\n");
			ui.NotifyUser("After logging in, please input the authorizationCode. This can be found in the address bar after redirection. It should be the code (everything) following the \"?code=\" portion of the URL");
			var authorizationCode = await ui.RequestResponseAsync("Please input the authorizationCode: ").WithoutContextCapture();
			return authorizationCode;
		}
	}

	public class SpotifyTestAccountAuthenticator : SpotifyAccountAuthenticator
	{
		protected override Task<string> RequestLoginFromUser(Uri loginUri, CancellationToken cancellationToken = default)
		{
			throw new InvalidOperationException("To authenticate in tests, a saved session must be provided");
		}
	}

	public record SessionDefinition(AuthorizationSource AuthSource, AuthorizationCodeTokensWrapper AuthTokens) : IOAuthInfo<AuthorizationSource, AuthorizationCodeTokensWrapper>
	{
		[JsonIgnore]
		public string SaveKey { get; set; }
	}
}