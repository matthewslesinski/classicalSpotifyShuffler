using System;
using SpotifyAPI.Web;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts
{
	public interface IAllLikedTracksPlaybackContext : ISpotifyPlaybackContext<FullTrack>
	{
		PlaybackContextType ISpotifyPlaybackContext<FullTrack>.ContextType => PlaybackContextType.AllLikedTracks;
		bool ISpotifyPlaybackContext<FullTrack>.TryGetSpotifyId(out string contextId)
		{
			contextId = null;
			return false;
		}
	}

	public interface IOriginalAllLikedTracksPlaybackContext : IAllLikedTracksPlaybackContext, IOriginalPlaybackContext<FullTrack>
	{
	}
}
