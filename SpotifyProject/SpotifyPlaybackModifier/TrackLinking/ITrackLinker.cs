using System;
using System.Collections.Generic;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using SpotifyProject.SpotifyPlaybackModifier.Transformations;

namespace SpotifyProject.SpotifyPlaybackModifier.TrackLinking
{
	public interface ITrackLinker<in ContextT, TrackT, out WorkT>
		where ContextT : ISpotifyPlaybackContext<TrackT>
	{
		IEnumerable<ITrackGrouping<WorkT, TrackT>> GroupTracksIntoWorks(ContextT originalContext, IEnumerable<TrackT> tracks);
	}
}
