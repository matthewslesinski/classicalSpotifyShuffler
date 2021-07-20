using System;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifyProject.Utils.Extensions;
using SpotifyProject.Setup;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackSetters;
using SpotifyProject.SpotifyPlaybackModifier.Transformations;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackModifiers
{
	public class OneTimeSpotifyPlaybackModifier<InputContextT, OutputContextT>
		: SpotifyPlaybackModifier<InputContextT, OutputContextT>
		where InputContextT : IOriginalPlaybackContext
		where OutputContextT : ISpotifyPlaybackContext
	{
		public OneTimeSpotifyPlaybackModifier(SpotifyConfiguration spotifyConfiguration, IPlaybackTransformation<InputContextT, OutputContextT> transformation,
			IPlaybackSetter<OutputContextT, PlaybackStateArgs> playbackSetter)
			: base(spotifyConfiguration, transformation, playbackSetter)
		{
		}

		public override Task Run(InputContextT context)
		{
			var maintainCurrentListening = Settings.Get<bool>(SettingsName.MaintainCurrentlyPlaying);
			return RunOnce(context, maintainCurrentListening);
		}

		private async Task RunOnce(InputContextT context, bool maintainCurrentListening = false)
		{
			await context.FullyLoad().WithoutContextCapture();
			var transformedContext = _transformer.Transform(context);
			PlaybackStateArgs playbackArgs = null;
			var currentMs = Environment.TickCount;
			var currentlyPlaying = await this.GetCurrentlyPlaying().WithoutContextCapture();
			var elapsedMs = Environment.TickCount - currentMs;
			if (maintainCurrentListening && currentlyPlaying?.Item is FullTrack currentlyPlayingTrack)
			{
				var uriToSetPlayTo = currentlyPlayingTrack.Uri;
				var positionToPlayAtMs = currentlyPlaying.ProgressMs.HasValue ? (currentlyPlaying.ProgressMs.Value + elapsedMs) : (int?) null;
				playbackArgs = new PlaybackStateArgs { UriToPlay = uriToSetPlayTo, PositionToPlayMs = positionToPlayAtMs };
			} else 
				playbackArgs = new PlaybackStateArgs();
			playbackArgs.CurrentPlaybackFound = currentlyPlaying?.Item != null;
			await _playbackSetter.SetPlayback(transformedContext, playbackArgs).WithoutContextCapture();
		}
	}
}
