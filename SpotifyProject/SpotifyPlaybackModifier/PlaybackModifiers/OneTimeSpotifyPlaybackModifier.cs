using System;
using System.Threading.Tasks;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using SpotifyProject.SpotifyPlaybackModifier.Transformations;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackModifiers
{
	public class OneTimeSpotifyPlaybackModifier<TrackT, InputContextT, OutputContextT, TransformT>
		: SpotifyPlaybackModifier<TrackT, InputContextT, OutputContextT, TransformT>,
		IOneTimePlaybackModifier<TrackT, InputContextT, OutputContextT, TransformT>
		where InputContextT : ISpotifyPlaybackContext<TrackT>
		where OutputContextT : ISpotifyPlaybackContext<TrackT>
		where TransformT : IPlaybackTransformation<InputContextT, OutputContextT, TrackT>
	{
		public OneTimeSpotifyPlaybackModifier(SpotifyConfiguration spotifyConfiguration, TransformT transformation, Func<TrackT, string> trackUriAccessor, Func<TrackT, bool> trackIsLocalAccessor)
			: base(spotifyConfiguration, transformation, trackUriAccessor, trackIsLocalAccessor)
		{
		}
	}
}
