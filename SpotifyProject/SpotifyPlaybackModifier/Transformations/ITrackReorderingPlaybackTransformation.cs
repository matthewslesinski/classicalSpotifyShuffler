using System;
using System.Collections.Generic;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;

namespace SpotifyProject.SpotifyPlaybackModifier.Transformations
{
	// TODO When the Mono default interface method bug is fixed, replace TrackReorderingPlaybackTransformationBase with ITrackReorderingPlaybackTransformation, and replace abstract override
	// methods with interface implementations in child classes

	//public interface ITrackReorderingPlaybackTransformation<in InputContextT, out OutputContextT, TrackT> : ICustomPlaybackTransformation<InputContextT, OutputContextT, TrackT>
	//	where InputContextT : ISpotifyPlaybackContext<TrackT>
	//	where OutputContextT : IReorderedPlaybackContext<TrackT, InputContextT>
	//{
	//	OutputContextT IPlaybackTransformation<InputContextT, OutputContextT>.Transform(InputContextT playbackContext)
	//	{
	//		var tracks = playbackContext.PlaybackOrder;
	//		var newTracks = Reorder(playbackContext, tracks);
	//		return ConstructNewContext(playbackContext, newTracks);
	//	}

	//	protected OutputContextT ConstructNewContext(InputContextT inputContext, IEnumerable<TrackT> newTrackOrder);

	//	protected IEnumerable<TrackT> Reorder(InputContextT originalContext, IEnumerable<TrackT> tracks);
	//}

	public abstract class TrackReorderingPlaybackTransformationBase<InputContextT, OutputContextT, TrackT> : ICustomPlaybackTransformation<InputContextT, OutputContextT, TrackT>
		where InputContextT : ISpotifyPlaybackContext<TrackT>
		where OutputContextT : IReorderedPlaybackContext<TrackT, InputContextT>
	{
		OutputContextT IPlaybackTransformation<InputContextT, OutputContextT>.Transform(InputContextT playbackContext)
		{
			var tracks = playbackContext.PlaybackOrder;
			var newTracks = Reorder(playbackContext, tracks);
			return ConstructNewContext(playbackContext, newTracks);
		}

		protected abstract OutputContextT ConstructNewContext(InputContextT inputContext, IEnumerable<TrackT> newTrackOrder);

		protected abstract IEnumerable<TrackT> Reorder(InputContextT originalContext, IEnumerable<TrackT> tracks);
	}
}
