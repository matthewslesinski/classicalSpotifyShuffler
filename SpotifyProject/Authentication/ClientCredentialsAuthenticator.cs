using System;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifyProject.SpotifyAdditions;

namespace SpotifyProject.Authentication
{
	/**
	 * A basic authenticator that only uses the client id and secret. This authenticator can not be used in a situation where access to spotify resources that require scopes (such
	 * as user information) is required
	 */
	public class ClientCredentialsAuthenticator : Authenticator
	{
		public ClientCredentialsAuthenticator(SpotifyClientConfig config) : base(config)
		{
		}

		protected override Task<IAuthenticator> GetAuthenticator(AuthorizationSource authorizationSource)
		{
			return Task.FromResult<IAuthenticator>(new SpotifyAPI.Web.ClientCredentialsAuthenticator(authorizationSource.ClientId, authorizationSource.ClientSecret));
		}
	}
}
