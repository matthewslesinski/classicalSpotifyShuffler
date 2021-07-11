using System;
using SpotifyAPI.Web;
using static SpotifyProject.Utils.SpotifyConstants;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts
{
	public interface IPlaylistPlaybackContext<TrackT> : IStaticPlaybackContext<FullPlaylist, TrackT>
	{
		PlaybackContextType ISpotifyPlaybackContext.ContextType => PlaybackContextType.Playlist;
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

		SpotifyElementType IStaticPlaybackContext<FullPlaylist, TrackT>.SpotifyElementType => SpotifyElementType.Playlist;
	}

	public interface IOriginalPlaylistPlaybackContext : IPlaylistPlaybackContext<FullTrack>, IOriginalPlaybackContext
	{ }
}
