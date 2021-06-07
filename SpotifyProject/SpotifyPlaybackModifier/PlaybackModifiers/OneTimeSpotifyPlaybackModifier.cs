using System;
using System.Threading.Tasks;
using SpotifyAPI.Web;
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
			var maintainCurrentListening = GlobalCommandLine.Store.GetOptionValue<bool>(CommandLineOptions.Names.MaintainCurrentlyPlaying);
			return RunOnce(context, maintainCurrentListening);
		}

		private async Task RunOnce(InputContextT context, bool maintainCurrentListening = false)
		{
			await context.FullyLoad();
			var transformedContext = _transformer.Transform(context);
			PlaybackStateArgs playbackArgs = null;
			if (maintainCurrentListening)
			{
				var currentMs = Environment.TickCount;
				var currentlyPlaying = await Spotify.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest { Market = SpotifyConfiguration.Market });
				var elapsedMs = Environment.TickCount - currentMs;
				if (currentlyPlaying?.Item is FullTrack currentlyPlayingTrack)
				{
					var uriToSetPlayTo = currentlyPlayingTrack.Uri;
					var positionToPlayAtMs = currentlyPlaying.ProgressMs.HasValue ? (currentlyPlaying.ProgressMs.Value + elapsedMs) : 0;
					playbackArgs = new PlaybackStateArgs { UriToPlay = uriToSetPlayTo, PositionToPlayMs = positionToPlayAtMs };
				}
			}
			await _playbackSetter.SetPlayback(transformedContext, playbackArgs);
		}
	}
}
