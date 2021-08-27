using System;
using System.Collections.Generic;
using ApplicationResources.Utils;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;
using SpotifyProject.Utils.Concepts;
using SpotifyProject.Utils.Extensions;

namespace SpotifyProject.SpotifyPlaybackModifier.Transformations
{
	public class SimpleWorkShuffle<InputContextT, OutputContextT, TrackT, WorkT> : IGroupingPlaybackTransformation<InputContextT, OutputContextT, TrackT, WorkT>
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

		OutputContextT ITrackReorderingPlaybackTransformation<InputContextT, OutputContextT, TrackT>.ConstructNewContext(InputContextT inputContext, IEnumerable<TrackT> newTrackOrder)
		{
			return _contextConstructor(inputContext, newTrackOrder);
		}

		IEnumerable<ITrackGrouping<WorkT, TrackT>> IGroupingPlaybackTransformation<InputContextT, OutputContextT, TrackT, WorkT>.GroupTracksIntoWorks(InputContextT playbackContext, IEnumerable<TrackT> tracks)
		{
			return _trackLinker.GroupTracksIntoWorks(playbackContext, tracks.DistinctOrdered(new KeyBasedEqualityComparer<TrackT, ITrackLinkingInfo>(playbackContext.GetMetadataForTrack, ITrackLinkingInfo.EqualityDefinition)));
		}

		IEnumerable<ITrackGrouping<WorkT, TrackT>> IGroupingPlaybackTransformation<InputContextT, OutputContextT, TrackT, WorkT>.ReorderWorks(IEnumerable<ITrackGrouping<WorkT, TrackT>> works)
		{
			return works.RandomShuffle();
		}
	}
}
