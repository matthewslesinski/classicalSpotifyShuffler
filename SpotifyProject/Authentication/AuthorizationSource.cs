using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using ApplicationResources.Services;

namespace SpotifyProject.Authentication
{
	/**
	 * Encapsulates the information needed when logging into spotify that acts like an identifier for access/refresh tokens.
	 * In other words, a given access/refresh token is only applicable to one AuthorizationSource, and if any of the values for a new
	 * AuthorizationSource are different, a new access/refresh token is required. This class should be expected to be serialized
	 */
	public class AuthorizationSource : IAuthCodeArgs, IOAuthClientInfo
	{
		public Uri RedirectUri { get; set; }
		public ClientInfo ClientInfo { get; set; }
		public ICollection<string> Scopes {
			get => _scopes;
			set => _scopes = value as ISet<string> ?? value.ToHashSet();
		}

		[JsonIgnore]
		public string RedirectUriString
		{
			get => RedirectUri.ToString();
			set => RedirectUri = new Uri(value);
		}
		[JsonIgnore]
		public string ScopeString
		{
			get => string.Join(' ', Scopes);
			set => Scopes = Scopes = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		}

		[JsonIgnore]
		public string ClientId => ClientInfo.ClientId;
		[JsonIgnore]
		public string ClientSecret => ClientInfo.ClientSecret;
		[JsonIgnore]
		public AuthenticationType AuthType => ClientInfo.AuthType;

		public override bool Equals(object obj)
		{
			return obj is AuthorizationSource o
				&& Equals(RedirectUri, o.RedirectUri)
				&& Equals(ClientInfo, o.ClientInfo)
				&& Scopes?.Count == o.Scopes?.Count
				&& Scopes?.Intersect(o.Scopes).Count() == Scopes?.Count;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(RedirectUri, ClientInfo, ScopeString);
		}

		private ISet<string> _scopes;
	}
}
