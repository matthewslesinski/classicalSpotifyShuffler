using System;
using ApplicationResources.Services;
using CustomResources.Utils.Concepts.DataStructures;
using Newtonsoft.Json;
using SpotifyAPI.Web;

namespace SpotifyProject.Authentication
{
	public record AuthorizationCodeTokensWrapper : IOAuthTokens, IWrapper<AuthorizationCodeTokenResponse>
	{
		public AuthorizationCodeTokenResponse TokenResponse { get; set; }

		[JsonIgnore]
		public string AccessToken => TokenResponse?.AccessToken;
		[JsonIgnore]
		public string RefreshToken => TokenResponse?.RefreshToken;

		[JsonIgnore]
		public AuthorizationCodeTokenResponse WrappedObject => TokenResponse;

		public static implicit operator AuthorizationCodeTokensWrapper(AuthorizationCodeTokenResponse tokenResponse) => new AuthorizationCodeTokensWrapper { TokenResponse = tokenResponse };
		public static implicit operator AuthorizationCodeTokenResponse(AuthorizationCodeTokensWrapper tokenWrapper) => tokenWrapper?.TokenResponse;
	}
}

