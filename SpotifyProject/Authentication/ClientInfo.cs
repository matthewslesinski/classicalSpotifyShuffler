using System;
namespace SpotifyProject.Authentication
{
	/**
	 * The Client Id and Secret used for logging in to spotify.
	 */
	public class ClientInfo
	{
		public string ClientId { get; set; }
		public string ClientSecret { get; set; }

		public override bool Equals(object obj)
		{
			return obj is ClientInfo o
				&& Equals(ClientId, o.ClientId)
				&& Equals(ClientSecret, o.ClientSecret);
		}

		public override int GetHashCode()
		{
			return (ClientId, ClientSecret).GetHashCode();
		}

		public override string ToString()
		{
			return (ClientId, ClientSecret).ToString();
		}
	}
}
