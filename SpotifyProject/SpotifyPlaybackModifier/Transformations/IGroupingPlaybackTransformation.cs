using System;
using System.Collections.Generic;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using System.Linq;

namespace SpotifyProject.SpotifyPlaybackModifier.Transformations
{
	// TODO When the Mono default interface method bug is fixed, replace TrackReorderingPlaybackTransformationBase with ITrackReorderingPlaybackTransformation, and replace abstract override
	// methods with interface implementations in child classes

	//public interface IGroupingPlaybackTransformation<in InputContextT, out OutputContextT, TrackT, WorkT> : TrackReorderingPlaybackTransformationBase<InputContextT, OutputContextT, TrackT>
	//	where InputContextT : ISpotifyPlaybackContext<TrackT>
	//	where OutputContextT : IReorderedPlaybackContext<TrackT, InputContextT>
	//{
	//	IEnumerable<TrackT> TrackReorderingPlaybackTransformationBase<InputContextT, OutputContextT, TrackT>.Reorder(InputContextT playbackContext, IEnumerable<TrackT> tracks)
	//	{
	//		var works = GroupTracksIntoWorks(playbackContext, tracks);
	//		var newOrder = ReorderWorks(works);
	//		return newOrder.SelectMany(grouping => grouping);
	//	}

	//	protected IEnumerable<ITrackGrouping<WorkT, TrackT>> GroupTracksIntoWorks(InputContextT playbackContext, IEnumerable<TrackT> tracks);

	//	protected IEnumerable<ITrackGrouping<WorkT, TrackT>> ReorderWorks(IEnumerable<ITrackGrouping<WorkT, TrackT>> works);

	//}


	// TODO When the Mono default interface method bug is fixed, replace GroupingPlaybackTransformationBase with IGroupingPlaybackTransformation, and replace abstract override
	// methods with interface implementations in child classes
	public abstract class GroupingPlaybackTransformationBase<InputContextT, OutputContextT, TrackT, WorkT> : TrackReorderingPlaybackTransformationBase<InputContextT, OutputContextT, TrackT>
		where InputContextT : ISpotifyPlaybackContext<TrackT>
		where OutputContextT : IReorderedPlaybackContext<TrackT, InputContextT>
	{
		protected override IEnumerable<TrackT> Reorder(InputContextT playbackContext, IEnumerable<TrackT> tracks)
		{
			var works = GroupTracksIntoWorks(playbackContext, tracks);
			var newOrder = ReorderWorks(works);
			return newOrder.SelectMany(grouping => grouping);
		}

		protected abstract IEnumerable<ITrackGrouping<WorkT, TrackT>> GroupTracksIntoWorks(InputContextT playbackContext, IEnumerable<TrackT> tracks);

		protected abstract IEnumerable<ITrackGrouping<WorkT, TrackT>> ReorderWorks(IEnumerable<ITrackGrouping<WorkT, TrackT>> works);

	}
}
