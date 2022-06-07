using System;
using System.IO;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using CustomResources.Utils.Extensions;
using SpotifyProject.SpotifyAdditions;
using ApplicationResources.ApplicationUtils;
using ApplicationResources.Logging;
using ApplicationResources.Utils;
using ApplicationResources.Services;
using CustomResources.Utils.Concepts;

namespace SpotifyProject.Authentication
{
	/**
	 * This class provides support for authentication with spotify through the AuthorizationCode procedure. This involves a client id and secret, a redirect uri, and a set of scopes to get
	 * permission for. Crucially, the AuthorizationCodeTokenResponse, containing among other things the access token and refresh token received from Spotify, will be saved to a file at
	 * credentialsFilePath, and if that file exists on startup, will be used instead of prompting the user for input. Note that this file will be rewritten everytime the refresh token is
	 * used to acquire a new access token. Therefore, this should theoretically only require logging in once, no matter when the containing program is run. If no file path is provided,
	 * the user will be required to log in everytime the containing program is run. Note that the user will also be required to log in again if a new AuthorizationSource is provided.
	 */
	public abstract class AuthorizationCodeAuthenticatorBase : Authenticator
	{
		private readonly string _credentialsFilePath;

		public AuthorizationCodeAuthenticatorBase(SpotifyClientConfig config, string credentialsFilePath) : base(config)
		{
			_credentialsFilePath = string.IsNullOrWhiteSpace(credentialsFilePath) ? null : credentialsFilePath;
		}

		protected override async Task<IAuthenticator> GetAuthenticator(AuthorizationSource authorizationSource)
		{
			SpotifyAuthenticationArguments BuildAuthenticationArgumentsFromTokenResponse(AuthorizationCodeTokenResponse tokenResponse) =>
				new SpotifyAuthenticationArguments { AuthorizationSource = authorizationSource, TokenResponse = tokenResponse};

			bool credentialsFileExists;
			SpotifyAuthenticationArguments authenticationArguments;
			bool askForLogin = false;
			if (_credentialsFilePath == null
				|| !((credentialsFileExists, authenticationArguments) = await ReadExistingAuthenticationArguments().WithoutContextCapture()).credentialsFileExists
				|| !Equals(authenticationArguments.AuthorizationSource, authorizationSource))
			{
				askForLogin = true;
				authenticationArguments = BuildAuthenticationArgumentsFromTokenResponse(await RequestInitialToken(authorizationSource).WithoutContextCapture());
			}

			var authenticator = new AuthorizationCodeAuthenticator(authorizationSource.ClientId, authorizationSource.ClientSecret, authenticationArguments.TokenResponse);
			if (_credentialsFilePath != null)
				authenticator.TokenRefreshed += (sender, token) => WriteTokenToFile(BuildAuthenticationArgumentsFromTokenResponse(token));
			if (_credentialsFilePath != null && askForLogin)
				_ = WriteTokenToFile(authenticationArguments).WrapInErrorHandler<Exception>(e => Logger.Error("An Exception occurred when writing credentials to {filePath}", _credentialsFilePath));
			return authenticator;
		}

		protected abstract Task<string> RequestLoginFromUser(Uri loginUri);

		private async Task<AuthorizationCodeTokenResponse> RequestInitialToken(AuthorizationSource authorizationSource)
		{
			var loginRequest = new LoginRequest(authorizationSource.RedirectUri, authorizationSource.ClientId, LoginRequest.ResponseType.Code)
			{
				Scope = authorizationSource.Scopes
			};
			var loginUri = loginRequest.ToUri();
			var authorizationCode = await RequestLoginFromUser(loginUri).WithoutContextCapture();
			var response = await new OAuthClient().RequestToken(new AuthorizationCodeTokenRequest(authorizationSource.ClientId, authorizationSource.ClientSecret, authorizationCode, authorizationSource.RedirectUri)).WithoutContextCapture();
			return response;
		}

		private Task<Result<SpotifyAuthenticationArguments>> ReadExistingAuthenticationArguments() =>
			ReadStoredData(_credentialsFilePath).Then(result => result.Transform(json =>
			{
				Logger.Verbose($"Reading Spotify access token from file {_credentialsFilePath}");
				return json.FromJsonString<SpotifyAuthenticationArguments>();
			}));
		

		private Task WriteTokenToFile(SpotifyAuthenticationArguments token)
		{
			Logger.Verbose($"Writing new Spotify access/refresh tokens to file {_credentialsFilePath}");
			var json = token.ToJsonString();
			return this.AccessLocalDataStore().SaveAsync(_credentialsFilePath, json);
		}
	}

	public class AuthorizationCodeCommandLineAuthenticator : AuthorizationCodeAuthenticatorBase
	{
		public AuthorizationCodeCommandLineAuthenticator(SpotifyClientConfig config, string credentialsFilePath) : base(config, credentialsFilePath)
		{
		}

		protected override async Task<string> RequestLoginFromUser(Uri loginUri)
		{
			var ui = this.AccessUserInterface();
			ui.NotifyUser($"Please go to the following address to login to Spotify: \n\n{loginUri}\n");
			ui.NotifyUser("After logging in, please input the authorizationCode. This can be found in the address bar after redirection. It should be the code (everything) following the \"?code=\" portion of the URL");
			var authorizationCode = await ui.RequestResponseAsync("Please input the authorizationCode: ").WithoutContextCapture();
			return authorizationCode;
		}
	}
}
