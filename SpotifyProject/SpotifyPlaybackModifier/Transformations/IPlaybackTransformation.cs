using System;
using System.Collections.Generic;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;

namespace SpotifyProject.SpotifyPlaybackModifier.Transformations
{
	public interface IPlaybackTransformation<in InputContextT, out OutputContextT>
		where InputContextT : ISpotifyPlaybackContext
		where OutputContextT : ISpotifyPlaybackContext
	{
		OutputContextT Transform(InputContextT playbackContext);
	}
}
