using System;
using System.Collections.Generic;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;

namespace SpotifyProject.SpotifyPlaybackModifier.Transformations
{
	public class SameOrdering<InputContextT, OutputContextT, TrackT> : ITrackReorderingPlaybackTransformation<InputContextT, OutputContextT, TrackT>
		where InputContextT : ISpotifyPlaybackContext<TrackT> where OutputContextT : IReorderedPlaybackContext<TrackT, InputContextT>
	{
		private readonly Func<InputContextT, IEnumerable<TrackT>, OutputContextT> _contextConstructor;

		public SameOrdering(Func<InputContextT, IEnumerable<TrackT>, OutputContextT> contextConstructor)
		{
			_contextConstructor = contextConstructor;
		}

		public OutputContextT ConstructNewContext(InputContextT inputContext, IEnumerable<TrackT> newTrackOrder)
		{
			return _contextConstructor(inputContext, newTrackOrder);
		}

		public IEnumerable<TrackT> Reorder(InputContextT originalContext, IEnumerable<TrackT> tracks)
		{
			return tracks;
		}
	}
}
