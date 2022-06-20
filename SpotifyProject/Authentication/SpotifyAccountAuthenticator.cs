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
using SpotifyProject.Utils;

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

		protected override async Task<Result<SessionDefinition>> GetExistingSession(CancellationToken cancellationToken = default)
		{
			var key = CurrentSessionKey;
			if (key == null)
				return Result<SessionDefinition>.NotFound;
			return await this.AccessLocalDataStore().TryGetAsync(key, CachePolicy.PreferActual, cancellationToken).Transform(json =>
			{
				Logger.Verbose($"Reading Spotify access token from {key}");
				var sessionInfo = json.FromJsonString<SessionDefinition>();
				sessionInfo.SaveKey = key;
				return sessionInfo;
			}).WithoutContextCapture();
		}

		protected override Task PersistCurrentSession(SessionDefinition sessionInfo, CancellationToken cancellationToken = default)
		{
			try
			{
				var key = sessionInfo?.SaveKey ?? CurrentSessionKey;
				if (key == null)
				{
					Logger.Warning("Not persisting state because there is no save key");
					return Task.CompletedTask;
				}

				Logger.Verbose($"Writing new Spotify access/refresh tokens to {key}");
				var json = sessionInfo?.ToJsonString();
				return this.AccessLocalDataStore().SaveAsync(key, json, CachePolicy.PreferActual, cancellationToken);
			}
			catch (Exception e)
			{
				Logger.Error("Failed to save session {sessionState} because of exception {exception}", sessionInfo, e);
				throw;
			}
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

		public static string CurrentSessionKey => Settings.TryGet<string>(BasicSettings.ProjectRootDirectory, out var root)
			&& Settings.TryGet<string>(SpotifySettings.PersonalDataDirectory, out var personalDataDir)
			&& TaskParameters.TryGet<string>(SpotifyParameters.TokenPath, out var tokenPath)
				? GeneralUtils.GetAbsoluteCombinedPath(root, personalDataDir, tokenPath)
				: null;
	}

	public abstract class SpotifyPKCEAuthenticator : SpotifyAccountAuthenticator
	{
		protected override Uri DetermineLoginAddress(AuthorizationSource userInfo)
		{
			var loginRequest = new LoginRequest(userInfo.RedirectUri, userInfo.ClientId, LoginRequest.ResponseType.Code)
			{
				CodeChallengeMethod = SpotifyConstants.PKCECodeChallengeMethod,
				CodeChallenge = PKCEUtil.GenerateCodes(userInfo.ClientSecret).challenge,
				Scope = userInfo.Scopes
			};
			return loginRequest.ToUri();
		}

		protected override Task<AuthorizationCodeTokensWrapper> AcquireTokens(AuthorizationSource userInfo, string authCode, CancellationToken cancellationToken = default) =>
			new OAuthClient().RequestToken(new PKCETokenRequest(userInfo.ClientId, authCode, userInfo.RedirectUri, userInfo.ClientSecret), cancellationToken)
					.Then(authTokens => new AuthorizationCodeTokensWrapper { TokenResponse = authTokens });

		protected override Task<IAuthenticator> GetAuthenticatedOutputFromTokens(SessionDefinition sessionInfo, CancellationToken _ = default)
		{
			var pkceAuthenticator = new PKCEAuthenticator(sessionInfo.AuthSource.ClientId, sessionInfo.AuthTokens.TokenResponse as PKCETokenResponse);
			if (sessionInfo.SaveKey != null)
				pkceAuthenticator.TokenRefreshed += (sender, token) => PersistCurrentSession(sessionInfo);
			return Task.FromResult<IAuthenticator>(pkceAuthenticator);
		}
	}

	public abstract class SpotifyAuthCodeAuthenticator : SpotifyAccountAuthenticator
	{
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
			var authCodeAuthenticator = new AuthorizationCodeAuthenticator(sessionInfo.AuthSource.ClientId, sessionInfo.AuthSource.ClientSecret, sessionInfo.AuthTokens.TokenResponse as AuthorizationCodeTokenResponse);
			if (sessionInfo.SaveKey != null)
				authCodeAuthenticator.TokenRefreshed += (sender, token) => PersistCurrentSession(sessionInfo);
			return Task.FromResult<IAuthenticator>(authCodeAuthenticator);
		}
	}

	public static class SpotifyComandLineAuthentication
	{
		public static async Task<Result<string>> RequestLoginFromUser(Uri loginUri, CancellationToken cancellationToken = default)
		{
			var ui = GlobalDependencies.Get<IUserInterface>();
			ui.NotifyUser($"Please go to the following address to login to Spotify: \n\n{loginUri}\n");
			ui.NotifyUser("After logging in, please input the authorizationCode. This can be found in the address bar after redirection. It should be the code (everything) following the \"?code=\" portion of the URL");
			var authorizationCode = await ui.RequestResponseAsync("Please input the authorizationCode: ").WithoutContextCapture();
			return (authorizationCode != null ? new(authorizationCode) : Result<string>.Failure);
		}
	}

	public record SessionDefinition(AuthorizationSource AuthSource, AuthorizationCodeTokensWrapper AuthTokens) : IOAuthInfo<AuthorizationSource, AuthorizationCodeTokensWrapper>
	{
		[JsonIgnore]
		public string SaveKey { get; set; }
	}


	#region Authenticator Implementations

	public class SpotifyCommandLineAuthCodeAuthenticator : SpotifyAuthCodeAuthenticator
	{
		protected override Task<Result<string>> RequestLoginFromUser(Uri loginUri, CancellationToken cancellationToken = default) =>
			SpotifyComandLineAuthentication.RequestLoginFromUser(loginUri, cancellationToken);
	}

	public class SpotifyCommandLinePKCEAuthenticator : SpotifyPKCEAuthenticator
	{
		protected override Task<Result<string>> RequestLoginFromUser(Uri loginUri, CancellationToken cancellationToken = default) =>
			SpotifyComandLineAuthentication.RequestLoginFromUser(loginUri, cancellationToken);
	}

	public class SpotifyTestAuthCodeAuthenticator : SpotifyAuthCodeAuthenticator
	{
		protected override Task<Result<string>> RequestLoginFromUser(Uri loginUri, CancellationToken cancellationToken = default)
		{
			throw new InvalidOperationException("To authenticate in tests, a saved session must be provided");
		}
	}

	public class SpotifyTestPKCEAuthenticator : SpotifyAuthCodeAuthenticator
	{
		protected override Task<Result<string>> RequestLoginFromUser(Uri loginUri, CancellationToken cancellationToken = default)
		{
			throw new InvalidOperationException("To authenticate in tests, a saved session must be provided");
		}
	}

	#endregion
}