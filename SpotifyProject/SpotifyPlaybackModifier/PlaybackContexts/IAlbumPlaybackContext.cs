using System;
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

		ITrackLinkingInfo<SimpleTrack> ISpotifyPlaybackContext<SimpleTrack>.GetMetadataForTrack(SimpleTrack track) => new SimpleTrackAndAlbumWrapper(track, SpotifyContext);
		SpotifyElementType IStaticPlaybackContext<FullAlbum, SimpleTrack>.SpotifyElementType => SpotifyElementType.Album;
	}

	public interface IOriginalAlbumPlaybackContext : IAlbumPlaybackContext, IOriginalPlaybackContext
	{
	}
}
