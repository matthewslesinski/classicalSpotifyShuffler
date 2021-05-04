using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts
{
	public interface ISpotifyPlaybackContext<TrackT> : ISpotifyQueue<TrackT>
	{
		PlaybackContextType ContextType { get; }
		bool TryGetSpotifyId(out string contextId);
		ITrackLinkingInfo GetMetadataForTrack(TrackT track);
	}


	public interface IProcessedTrackPlaybackContext<TrackT, InfoT> : ISpotifyPlaybackContext<InfoT> where InfoT : ITrackLinkingInfo<TrackT>
	{
		ITrackLinkingInfo ISpotifyPlaybackContext<InfoT>.GetMetadataForTrack(InfoT track) => track;
	}

	public enum PlaybackContextType
	{
		Undefined,
		Album,
		Artist,
		Playlist,
		AllLikedTracks,
		CustomQueue
	}
}
