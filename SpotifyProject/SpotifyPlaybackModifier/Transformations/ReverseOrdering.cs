﻿using System;
using System.Collections.Generic;
using CustomResources.Utils.Extensions;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;

namespace SpotifyProject.SpotifyPlaybackModifier.Transformations
{
	// TODO When the Mono default interface method bug is fixed, replace TrackReorderingPlaybackTransformationBase with ITrackReorderingPlaybackTransformation, and replace abstract override
	// methods with interface implementations in child classes
	public class ReverseOrdering<InputContextT, OutputContextT, TrackT> : TrackReorderingPlaybackTransformationBase<InputContextT, OutputContextT, TrackT>
		where InputContextT : ISpotifyPlaybackContext<TrackT> where OutputContextT : IReorderedPlaybackContext<TrackT, InputContextT>
	{
		private readonly Func<InputContextT, IEnumerable<TrackT>, OutputContextT> _contextConstructor;

		public ReverseOrdering(Func<InputContextT, IEnumerable<TrackT>, OutputContextT> contextConstructor)
		{
			_contextConstructor = contextConstructor;
		}

		protected override OutputContextT ConstructNewContext(InputContextT inputContext, IEnumerable<TrackT> newTrackOrder)
		{
			return _contextConstructor(inputContext, newTrackOrder);
		}

		protected override IEnumerable<TrackT> Reorder(InputContextT originalContext, IEnumerable<TrackT> tracks)
		{
			return tracks.Reversed();
		}
	}
}
