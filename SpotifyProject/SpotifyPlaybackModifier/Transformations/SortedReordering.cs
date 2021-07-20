using System;
using System.Linq;
using System.Collections.Generic;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using SpotifyProject.Utils.Concepts;
using SpotifyProject.Utils.Extensions;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;

namespace SpotifyProject.SpotifyPlaybackModifier.Transformations
{
	public class SortedReordering<InputContextT, OutputContextT, TrackT> : ITrackReorderingPlaybackTransformation<InputContextT, OutputContextT, TrackT>
		where InputContextT : ISpotifyPlaybackContext<TrackT> where OutputContextT : IReorderedPlaybackContext<TrackT, InputContextT>
	{
		private readonly Func<InputContextT, IEnumerable<TrackT>, OutputContextT> _contextConstructor;
		private readonly IComparer<ITrackLinkingInfo> _intendedOrder;

		public SortedReordering(Func<InputContextT, IEnumerable<TrackT>, OutputContextT> contextConstructor, IComparer<ITrackLinkingInfo> intendedOrder)
		{
			_contextConstructor = contextConstructor;
			_intendedOrder = intendedOrder;
		}

		OutputContextT ITrackReorderingPlaybackTransformation<InputContextT, OutputContextT, TrackT>.ConstructNewContext(InputContextT inputContext, IEnumerable<TrackT> newTrackOrder)
		{
			return _contextConstructor(inputContext, newTrackOrder);
		}

		IEnumerable<TrackT> ITrackReorderingPlaybackTransformation<InputContextT, OutputContextT, TrackT>.Reorder(InputContextT originalContext, IEnumerable<TrackT> tracks)
		{
			return tracks.OrderBy(ComparerUtils.ComparingBy<TrackT, ITrackLinkingInfo>(originalContext.GetMetadataForTrack, _intendedOrder));
		}
	}
}
