using System;
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

		public static readonly char UriPartDivider = ':';

	}
}
