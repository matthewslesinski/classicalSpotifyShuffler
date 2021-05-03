using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using SpotifyProject.SpotifyPlaybackModifier.Transformations;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackModifiers
{
	public interface IPlaybackModifier<TrackT, in InputContextT, in OutputContextT, out TransformT> : ISpotifyAccessor
		where InputContextT : ISpotifyPlaybackContext<TrackT>
		where OutputContextT : ISpotifyPlaybackContext<TrackT>
		where TransformT : IPlaybackTransformation<InputContextT, OutputContextT, TrackT>
	{

		Task SetCurrentPlaybackContext(OutputContextT context, string uriToPlay = null, int positionMs = 0);
		TransformT Transformer { get; }
	}
}
