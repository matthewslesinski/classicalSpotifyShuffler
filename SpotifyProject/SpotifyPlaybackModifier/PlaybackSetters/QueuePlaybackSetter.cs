using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifyProject.Setup;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackSetters
{
	public class QueuePlaybackSetter<TrackT> : SpotifyAccessorBase, IPlaybackSetter<ISpotifyPlaybackContext<TrackT>, PlaybackStateArgs>
	{
		public QueuePlaybackSetter(SpotifyConfiguration spotifyConfiguration) : base(spotifyConfiguration)
		{ }

		public async Task SetPlayback(ISpotifyPlaybackContext<TrackT> context, PlaybackStateArgs args)
		{
			if (args.CurrentPlaybackFound.HasValue && !args.CurrentPlaybackFound.Value)
				throw new ArgumentException("Cannot modify playback when nothing is playing");
			string uriToPlay = args.UriToPlay;
			int? positionMs = args.PositionToPlayMs;
			var allowLocalTracks = args.AllowUsingContextUri ?? false;
			var allowUsingContextUri = args.AllowUsingContextUri ?? false;
			var useContextUri = context.TryGetSpotifyUri(out var contextUri) && allowUsingContextUri;
			bool IsAllowedTrack(TrackT track)
			{
				var isLocal = context.IsLocal(track);
				var isAllowed = !isLocal || useContextUri;
				if (!isAllowed)
					Logger.Warning($"Not including track {context.GetUriForTrack(track)} because it is a local track and spotify doesn't support them through their API");
				return isAllowed;
			}

			var uris = context.PlaybackOrder
				.Where(IsAllowedTrack)
				.Select(context.GetUriForTrack).ToList();
			var trackLimit = Settings.Get<int>(SettingsName.TrackQueueSizeLimit);
			var limitUris = !useContextUri && trackLimit < uris.Count();
			var useUri = uriToPlay != null && uris.Contains(uriToPlay);
			List<string> trackUris;

			if (limitUris)
			{
				if (useUri)
				{

					var index = uris.IndexOf(uriToPlay);
					var amountToTakeBefore = trackLimit / 2;
					var amountToTakeAfter = (trackLimit + 1) / 2;
					var startIndex = index < amountToTakeBefore ? 0 : (index > uris.Count - amountToTakeAfter ? uris.Count - trackLimit : index - amountToTakeBefore);
					trackUris = uris.GetRange(startIndex, trackLimit);
				}
				else
					trackUris = uris.GetRange(0, trackLimit);
			}
			else
				trackUris = uris;

			if (useContextUri)
				Logger.Information($"Setting playback to context with uri: {contextUri}");
			else
				Logger.Information($"Setting playback to {(limitUris ? $"{trackLimit} out of {uris.Count}" : uris.Count.ToString())} tracks");

			var playbackRequest = new PlayerResumePlaybackRequest
			{
				ContextUri = useContextUri ? contextUri : default,
				Uris = useContextUri ? default : trackUris,
				OffsetParam = useUri ? new PlayerResumePlaybackRequest.Offset { Uri = uriToPlay } : default,
				PositionMs = useUri ? positionMs : default
			};
			await this.SetCurrentPlayback(playbackRequest);
			await this.SetShuffle(false);
			Logger.Information(useUri ? "The playback queue should be different" : "Should be something new playing");
		}
	}
}
