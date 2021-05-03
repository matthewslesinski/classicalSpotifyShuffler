using System;
using System.Collections.Generic;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using System.Linq;

namespace SpotifyProject.SpotifyPlaybackModifier.Transformations
{
	public interface IGroupingPlaybackTransformation<in InputContextT, out OutputContextT, TrackT, WorkT> : ITrackReorderingPlaybackTransformation<InputContextT, OutputContextT, TrackT>
		where InputContextT : ISpotifyPlaybackContext<TrackT>
		where OutputContextT : IReorderedPlaybackContext<TrackT, InputContextT>
	{
		IEnumerable<TrackT> ITrackReorderingPlaybackTransformation<InputContextT, OutputContextT, TrackT>.Reorder(InputContextT playbackContext, IEnumerable<TrackT> tracks)
		{
			var works = GroupTracksIntoWorks(playbackContext, tracks);
			var newOrder = ReorderWorks(works);
			return newOrder.SelectMany(grouping => grouping);
		}

		IEnumerable<ITrackGrouping<WorkT, TrackT>> GroupTracksIntoWorks(InputContextT playbackContext, IEnumerable<TrackT> tracks);

		IEnumerable<ITrackGrouping<WorkT, TrackT>> ReorderWorks(IEnumerable<ITrackGrouping<WorkT, TrackT>> works);

	}
}
