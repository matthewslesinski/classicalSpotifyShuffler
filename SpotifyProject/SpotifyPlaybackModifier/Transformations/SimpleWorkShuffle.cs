using System;
using System.Collections.Generic;
using ApplicationResources.Utils;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;
using CustomResources.Utils.Concepts;
using CustomResources.Utils.Extensions;

namespace SpotifyProject.SpotifyPlaybackModifier.Transformations
{

	// TODO When the Mono default interface method bug is fixed, replace GroupingPlaybackTransformationBase with IGroupingPlaybackTransformation, and replace abstract override
	// methods with interface implementations in child classes
	public class SimpleWorkShuffle<InputContextT, OutputContextT, TrackT, WorkT> : GroupingPlaybackTransformationBase<InputContextT, OutputContextT, TrackT, WorkT>
		where InputContextT : ISpotifyPlaybackContext<TrackT>
		where OutputContextT : IReorderedPlaybackContext<TrackT, InputContextT>
	{
		private readonly Func<InputContextT, IEnumerable<TrackT>, OutputContextT> _contextConstructor;
		private readonly ITrackLinker<InputContextT, TrackT, WorkT> _trackLinker;

		public SimpleWorkShuffle(Func<InputContextT, IEnumerable<TrackT>, OutputContextT> contextConstructor, ITrackLinker<InputContextT, TrackT, WorkT> trackLinker)
		{
			_contextConstructor = contextConstructor;
			_trackLinker = trackLinker;
		}

		protected override OutputContextT ConstructNewContext(InputContextT inputContext, IEnumerable<TrackT> newTrackOrder)
		{
			return _contextConstructor(inputContext, newTrackOrder);
		}

		protected override IEnumerable<ITrackGrouping<WorkT, TrackT>> GroupTracksIntoWorks(InputContextT playbackContext, IEnumerable<TrackT> tracks)
		{
			return _trackLinker.GroupTracksIntoWorks(playbackContext, tracks.DistinctOrdered(new KeyBasedEqualityComparer<TrackT, ITrackLinkingInfo>(playbackContext.GetMetadataForTrack, ITrackLinkingInfo.EqualityDefinition)));
		}

		protected override IEnumerable<ITrackGrouping<WorkT, TrackT>> ReorderWorks(IEnumerable<ITrackGrouping<WorkT, TrackT>> works)
		{
			return works.RandomShuffle();
		}
	}
}
