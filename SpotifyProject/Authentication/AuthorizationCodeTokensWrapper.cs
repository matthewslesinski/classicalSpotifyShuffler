using System;
using ApplicationResources.Services;
using CustomResources.Utils.Concepts.DataStructures;
using Newtonsoft.Json;
using SpotifyAPI.Web;

namespace SpotifyProject.Authentication
{
	public record AuthorizationCodeTokensWrapper : IOAuthTokens, IWrapper<IRefreshableToken>
	{
		public IRefreshableToken TokenResponse { get; set; }

		[JsonIgnore]
		public string AccessToken => TokenResponse?.AccessToken;
		[JsonIgnore]
		public string RefreshToken => TokenResponse?.RefreshToken;

		[JsonIgnore]
		public IRefreshableToken WrappedObject => TokenResponse;
	}
}

