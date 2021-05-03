using System;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using SpotifyProject.SpotifyPlaybackModifier.Transformations;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackModifiers
{
	public interface IOneTimePlaybackModifier<TrackT, in InputContextT, in OutputContextT, TransformT> : IPlaybackModifier<TrackT, InputContextT, OutputContextT, TransformT>
		where InputContextT : ISpotifyPlaybackContext<TrackT>
		where OutputContextT : ISpotifyPlaybackContext<TrackT>
		where TransformT : IPlaybackTransformation<InputContextT, OutputContextT, TrackT>
	{

		public async Task RunOnce(InputContextT context, bool maintainCurrentListening = false)
		{
			var transformedContext = Transformer.Transform(context);
			string uriToSetPlayTo = null;
			int positionToPlayAtMs = 0;
			if (maintainCurrentListening)
			{
				var currentMs = Environment.TickCount;
				var currentlyPlaying = await Spotify.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest { Market = SpotifyConfiguration.Market });
				var elapsedMs = Environment.TickCount - currentMs;
				uriToSetPlayTo = ((FullTrack)currentlyPlaying.Item).Uri;
				positionToPlayAtMs = currentlyPlaying.ProgressMs.HasValue ? (currentlyPlaying.ProgressMs.Value + elapsedMs) : 0;
			}
			await SetCurrentPlaybackContext(transformedContext, uriToSetPlayTo, positionToPlayAtMs);
		}
	}
}
