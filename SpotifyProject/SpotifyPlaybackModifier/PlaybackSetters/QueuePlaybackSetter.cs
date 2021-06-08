using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifyProject.Setup;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackSetters
{
	public class QueuePlaybackSetter<TrackT> : SpotifyAccessorBase, IPlaybackSetter<ISpotifyPlaybackContext<TrackT>, PlaybackStateArgs>
	{
		public QueuePlaybackSetter(SpotifyConfiguration spotifyConfiguration) : base(spotifyConfiguration)
		{
		}

		public async Task SetPlayback(ISpotifyPlaybackContext<TrackT> context, PlaybackStateArgs args)
		{
			string uriToPlay = args?.UriToPlay;
			int positionMs = args?.PositionToPlayMs ?? 0;
			bool GetIsNotLocalTrack(TrackT track)
			{
				var isLocal = context.IsLocal(track);
				if (isLocal)
					Logger.Warning($"Not including track {context.GetUriForTrack(track)} because it is a local track and spotify doesn't support them through their API");
				return !isLocal;
			}

			var uris = context.PlaybackOrder
				.Where(GetIsNotLocalTrack)
				.Select(context.GetUriForTrack).ToList();
			var trackLimit = GlobalCommandLine.Store.GetOptionValue<int>(CommandLineOptions.Names.TrackQueueSizeLimit);
			var limitUris = trackLimit < uris.Count();
			var useUri = uriToPlay != null && uris.Contains(uriToPlay);
			Logger.Information($"Setting {(limitUris ? $"{trackLimit} out of {uris.Count()}" : uris.Count().ToString())} tracks");

			List<string> trackUris;

			if (limitUris && useUri)
			{
				var index = uris.IndexOf(uriToPlay);
				var amountToTakeBefore = trackLimit / 2;
				var amountToTakeAfter = (trackLimit + 1) / 2;
				var startIndex = index < amountToTakeBefore ? 0 : (index > uris.Count - amountToTakeAfter ? uris.Count - trackLimit : index - amountToTakeBefore);
				trackUris = uris.GetRange(startIndex, trackLimit);
			}
			else
				trackUris = uris;

			var playbackRequest = new PlayerResumePlaybackRequest
			{
				Uris = trackUris,
				OffsetParam = useUri ? new PlayerResumePlaybackRequest.Offset { Uri = uriToPlay } : default,
				PositionMs = useUri ? positionMs : default
			};
			await Spotify.Player.ResumePlayback(playbackRequest);
			await Spotify.Player.SetShuffle(new PlayerShuffleRequest(false));
			Logger.Information(useUri ? "The playback queue should be different" : "Should be something new playing");
		}
	}
}
