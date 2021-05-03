using System;
using System.Collections.Generic;
using SpotifyAPI.Web;
using SpotifyProject.Utils;

namespace SpotifyProject.Authentication
{
	/**
	 * An authentication state, including the requirements for logging in as well as the access/refresh token for that AuthorizationSource.
	 * This class enables succinctly persisting login information between sessions through JSON serialization, so the user doesn't need to repeatedly log in.
	 */
	public class SpotifyAuthenticationArguments
	{
		public AuthorizationSource AuthorizationSource {get; set; }
		public AuthorizationCodeTokenResponse TokenResponse { get; set; }

		public override bool Equals(object obj)
		{
			return obj is SpotifyAuthenticationArguments o
				&& Equals(AuthorizationSource, o.AuthorizationSource)
				&& TokenResponse.IsIdenticalTo(o.TokenResponse);
		}

		public override int GetHashCode()
		{
			return (AuthorizationSource, TokenResponse.Hash()).GetHashCode();
		}
	}
}
