using System;
using System.Collections.Generic;
using CustomResources.Utils.Concepts;
using SpotifyAPI.Web;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;
using static SpotifyProject.Utils.SpotifyConstants;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts
{
	public interface IAlbumPlaybackContext : IStaticPlaybackContext<FullAlbum, SimpleTrack>
	{
		PlaybackContextType ISpotifyPlaybackContext.ContextType => PlaybackContextType.Album;
		bool ISpotifyPlaybackContext.TryGetSpotifyId(out string contextId)
		{
			contextId = SpotifyContext.Id;
			return true;
		}
		bool ISpotifyPlaybackContext.TryGetSpotifyUri(out string contextUri)
		{
			contextUri = SpotifyContext.Uri;
			return true;
		}

		IPlayableTrackLinkingInfo<SimpleTrack> ISpotifyPlaybackContext<SimpleTrack>.GetMetadataForTrack(SimpleTrack track) => new SimpleTrackAndAlbumWrapper(track, SpotifyContext);
		SpotifyElementType IStaticPlaybackContext<FullAlbum, SimpleTrack>.SpotifyElementType => SpotifyElementType.Album;

		public static readonly IEqualityComparer<SimpleAlbum> SimpleAlbumEqualityComparer =
			new KeyBasedEqualityComparer<SimpleAlbum, (string, string, string, int?)>(album => (album?.Name, album?.ReleaseDate, album?.AlbumType, album?.TotalTracks));

		public static readonly IEqualityComparer<FullAlbum> FullAlbumEqualityComparer =
			new KeyBasedEqualityComparer<FullAlbum, (string, string, string, int?)>(album => (album?.Name, album?.ReleaseDate, album?.AlbumType, album?.TotalTracks));
	}

	public interface IOriginalAlbumPlaybackContext : IAlbumPlaybackContext, IOriginalPlaybackContext
	{
	}
}
