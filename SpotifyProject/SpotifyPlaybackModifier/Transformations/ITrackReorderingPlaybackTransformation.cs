using System;
using System.Collections.Generic;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;

namespace SpotifyProject.SpotifyPlaybackModifier.Transformations
{
	public interface ITrackReorderingPlaybackTransformation<in InputContextT, out OutputContextT, TrackT> : ICustomPlaybackTransformation<InputContextT, OutputContextT, TrackT>
		where InputContextT : ISpotifyPlaybackContext<TrackT>
		where OutputContextT : IReorderedPlaybackContext<TrackT, InputContextT>
	{
		OutputContextT IPlaybackTransformation<InputContextT, OutputContextT, TrackT>.Transform(InputContextT playbackContext)
		{
			var tracks = playbackContext.PlaybackOrder;
			var newTracks = Reorder(playbackContext, tracks);
			return ConstructNewContext(playbackContext, newTracks);
		}

		OutputContextT ConstructNewContext(InputContextT inputContext, IEnumerable<TrackT> newTrackOrder);

		IEnumerable<TrackT> Reorder(InputContextT originalContext, IEnumerable<TrackT> tracks);
	}
}
