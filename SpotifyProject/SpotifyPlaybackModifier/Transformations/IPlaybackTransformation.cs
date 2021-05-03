using System;
using System.Collections.Generic;
using SpotifyAPI.Web;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;

namespace SpotifyProject.SpotifyPlaybackModifier.Transformations
{
	public interface IPlaybackTransformation<in InputContextT, out OutputContextT, TrackT>
		where InputContextT : ISpotifyPlaybackContext<TrackT>
		where OutputContextT : ISpotifyPlaybackContext<TrackT>
	{
		OutputContextT Transform(InputContextT playbackContext);
	}
}
