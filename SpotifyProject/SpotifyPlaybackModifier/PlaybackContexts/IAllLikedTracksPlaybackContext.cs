using System;
using SpotifyAPI.Web;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts
{
	public interface IAllLikedTracksPlaybackContext : ISpotifyPlaybackContext<FullTrack>
	{
		PlaybackContextType ISpotifyPlaybackContext.ContextType => PlaybackContextType.AllLikedTracks;
		bool ISpotifyPlaybackContext.TryGetSpotifyId(out string contextId)
		{
			contextId = null;
			return false;
		}
		bool ISpotifyPlaybackContext.TryGetSpotifyUri(out string contextUri)
		{
			contextUri = null;
			return false;
		}

	}

	public interface IOriginalAllLikedTracksPlaybackContext : IAllLikedTracksPlaybackContext, IOriginalPlaybackContext
	{
	}
}
