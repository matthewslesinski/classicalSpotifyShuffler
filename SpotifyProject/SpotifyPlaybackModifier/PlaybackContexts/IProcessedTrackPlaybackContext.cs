﻿using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts
{
	public interface IProcessedTrackPlaybackContext<TrackT, InfoT> : ISpotifyPlaybackContext<InfoT> where InfoT : ITrackLinkingInfo<TrackT>, IPlayableTrackLinkingInfo
	{
		IPlayableTrackLinkingInfo<InfoT> ISpotifyPlaybackContext<InfoT>.GetMetadataForTrack(InfoT track) => new TrackLinkingInfoWrapper<TrackT, InfoT>(track);
	}
}
