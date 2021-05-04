using System;
using SpotifyAPI.Web;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts
{
	public interface IPlaylistPlaybackContext : IStaticPlaybackContext<FullPlaylist, FullTrack>
	{
		PlaybackContextType ISpotifyPlaybackContext<FullTrack>.ContextType => PlaybackContextType.Playlist;
		bool ISpotifyPlaybackContext<FullTrack>.TryGetSpotifyId(out string contextId)
		{
			contextId = SpotifyContext.Id;
			return true;
		}
	}

	public interface IOriginalPlaylistPlaybackContext : IPlaylistPlaybackContext, IOriginalPlaybackContext<FullTrack>
	{

	}
}
