using System;
using System.Collections.Generic;
using System.Net;
using SpotifyAPI.Web;

namespace SpotifyProject.Utils
{
	public static class SpotifyConstants
	{
		public static readonly string[] AllAuthenticationScopes = new[] {
			Scopes.AppRemoteControl, Scopes.PlaylistModifyPrivate, Scopes.PlaylistModifyPublic, Scopes.PlaylistReadCollaborative,
			Scopes.PlaylistReadPrivate, Scopes.Streaming, Scopes.UgcImageUpload, Scopes.UserFollowModify, Scopes.UserFollowRead,
			Scopes.UserLibraryModify, Scopes.UserLibraryRead, Scopes.UserModifyPlaybackState, Scopes.UserReadCurrentlyPlaying, Scopes.UserReadEmail,
			Scopes.UserReadPlaybackPosition, Scopes.UserReadPlaybackState, Scopes.UserReadPrivate, Scopes.UserReadRecentlyPlayed, Scopes.UserTopRead
		};

		public const char UriPartDivider = ':';
		public const string OpenSpotifyUrl = "https://open.spotify.com";
		public const string SpotifyUriPrefix = "spotify:";

		public const int PlaylistRequestBatchSize = 100;
		public const int PlaylistSizeLimit = 10000;

		public const int APIRateLimitWindowMS = 30000;

		public const string USMarket = "US";

		public enum SpotifyElementType
		{
			Album,
			Playlist,
			Track,
			Artist
		}

		public static IReadOnlySet<HttpStatusCode> NonDeterministicStatusCodes { get; } = new HashSet<HttpStatusCode>
		{
				HttpStatusCode.InternalServerError,
				HttpStatusCode.BadGateway,
				HttpStatusCode.GatewayTimeout,
				HttpStatusCode.ServiceUnavailable
		};
	}
}
