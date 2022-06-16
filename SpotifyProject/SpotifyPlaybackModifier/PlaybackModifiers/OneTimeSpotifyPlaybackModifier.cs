using System;
using System.Threading.Tasks;
using CustomResources.Utils.Extensions;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackSetters;
using SpotifyProject.SpotifyPlaybackModifier.Transformations;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackModifiers
{
	public class OneTimeSpotifyPlaybackModifier<InputContextT, OutputContextT>
		: SpotifyPlaybackModifier<InputContextT, OutputContextT>
		where InputContextT : ISpotifyPlaybackContext
		where OutputContextT : ISpotifyPlaybackContext
	{
		public OneTimeSpotifyPlaybackModifier(SpotifyConfiguration spotifyConfiguration, IPlaybackTransformation<InputContextT, OutputContextT> transformation,
			IContextSetter<OutputContextT, PlaybackStateArgs> playbackSetter)
			: base(spotifyConfiguration, transformation, playbackSetter)
		{
		}

		public override Task Run(InputContextT context)
		{
			return RunOnce(context);
		}

		private async Task RunOnce(InputContextT context)
		{
			var transformedContext = _transformer.Transform(context);
			await _playbackSetter.SetContext(transformedContext, new PlaybackStateArgs()).WithoutContextCapture();
		}
	}
}
