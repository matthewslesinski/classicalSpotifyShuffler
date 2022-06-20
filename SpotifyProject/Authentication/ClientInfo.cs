using System;
using ApplicationResources.Services;

namespace SpotifyProject.Authentication
{
	/**
	 * The Client Id and Secret used for logging in to spotify.
	 */
	public record ClientInfo : IOAuthClientInfo
	{
		public string ClientId { get; set; }
		public string ClientSecret { get; set; }
		public AuthenticationType AuthType { get; set; }
	}
}
