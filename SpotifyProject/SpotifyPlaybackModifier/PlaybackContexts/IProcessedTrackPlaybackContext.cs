using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts
{
	public interface IProcessedTrackPlaybackContext<InfoT> : ISpotifyPlaybackContext<InfoT> where InfoT : IPlayableTrackLinkingInfo
	{
		IPlayableTrackLinkingInfo<InfoT> ISpotifyPlaybackContext<InfoT>.GetMetadataForTrack(InfoT track) => new TrackLinkingInfoWrapper<InfoT>(track);
	}
}
