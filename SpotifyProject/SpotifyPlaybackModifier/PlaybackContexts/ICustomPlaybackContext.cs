using System;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts
{
	public interface ICustomPlaybackContext : ISpotifyPlaybackContext<IPlayableTrackLinkingInfo>, IProcessedTrackPlaybackContext<IPlayableTrackLinkingInfo>
	{
		PlaybackContextType ISpotifyPlaybackContext.ContextType => PlaybackContextType.CustomQueue;
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
}

