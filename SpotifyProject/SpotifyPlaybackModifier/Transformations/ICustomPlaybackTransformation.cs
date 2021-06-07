using System;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;

namespace SpotifyProject.SpotifyPlaybackModifier.Transformations
{
	public interface ICustomPlaybackTransformation<in InputContextT, out OutputContextT, TrackT> : IPlaybackTransformation<InputContextT, OutputContextT>
		where InputContextT : ISpotifyPlaybackContext<TrackT>
		where OutputContextT : IReorderedPlaybackContext<TrackT, InputContextT>
	{
	}
}
