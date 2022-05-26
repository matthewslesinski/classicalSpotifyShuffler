﻿using System;
using System.Linq;
using System.Collections.Generic;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using CustomResources.Utils.Concepts;
using CustomResources.Utils.Extensions;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;

namespace SpotifyProject.SpotifyPlaybackModifier.Transformations
{
	// TODO When the Mono default interface method bug is fixed, replace TrackReorderingPlaybackTransformationBase with ITrackReorderingPlaybackTransformation, and replace abstract override
	// methods with interface implementations in child classes
	public class SortedReordering<InputContextT, OutputContextT, TrackT> : TrackReorderingPlaybackTransformationBase<InputContextT, OutputContextT, TrackT>
		where InputContextT : ISpotifyPlaybackContext<TrackT> where OutputContextT : IReorderedPlaybackContext<TrackT, InputContextT>
	{
		private readonly Func<InputContextT, IEnumerable<TrackT>, OutputContextT> _contextConstructor;
		private readonly IComparer<ITrackLinkingInfo> _intendedOrder;

		public SortedReordering(Func<InputContextT, IEnumerable<TrackT>, OutputContextT> contextConstructor, IComparer<ITrackLinkingInfo> intendedOrder)
		{
			_contextConstructor = contextConstructor;
			_intendedOrder = intendedOrder;
		}

		protected override OutputContextT ConstructNewContext(InputContextT inputContext, IEnumerable<TrackT> newTrackOrder)
		{
			return _contextConstructor(inputContext, newTrackOrder);
		}

		protected override IEnumerable<TrackT> Reorder(InputContextT originalContext, IEnumerable<TrackT> tracks)
		{
			return tracks.OrderBy(ComparerUtils.ComparingBy<TrackT, ITrackLinkingInfo>(originalContext.GetMetadataForTrack, _intendedOrder));
		}
	}
}
