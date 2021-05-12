using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using System.Linq;
using SpotifyProject.SpotifyPlaybackModifier.Transformations;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using SpotifyProject.Setup;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackModifiers
{
	public abstract class SpotifyPlaybackModifier<TrackT, InputContextT, OutputContextT, TransformT> : SpotifyAccessorBase, IPlaybackModifier<TrackT, InputContextT, OutputContextT, TransformT>
		where InputContextT : ISpotifyPlaybackContext<TrackT>
		where OutputContextT : ISpotifyPlaybackContext<TrackT>
		where TransformT : IPlaybackTransformation<InputContextT, OutputContextT, TrackT>
	{
		private readonly Func<TrackT, string> _trackUriAccessor;
		private readonly Func<TrackT, bool> _trackIsLocalAccessor;
		public SpotifyPlaybackModifier(SpotifyConfiguration spotifyConfiguration, TransformT transformation, Func<TrackT, string> trackUriAccessor, Func<TrackT, bool> trackIsLocalAccessor) : base(spotifyConfiguration)
		{
			_transformer = transformation;
			_trackUriAccessor = trackUriAccessor;
			_trackIsLocalAccessor = trackIsLocalAccessor;
		}

		TransformT IPlaybackModifier<TrackT, InputContextT, OutputContextT, TransformT>.Transformer => _transformer;
		private readonly TransformT _transformer;

		protected virtual string GetUriFromTrack(TrackT track) => _trackUriAccessor(track);

		protected virtual bool GetIsLocalTrack(TrackT track) {
			var isLocal = _trackIsLocalAccessor(track);
			if (isLocal)
				Logger.Warning($"Not including track {GetUriFromTrack(track)} because it is a local track and spotify doesn't support them through their API");
			return isLocal;
		}

		public async Task SetCurrentPlaybackContext(OutputContextT context, string uriToPlay = null, int positionMs = 0)
		{
			var uris = context.PlaybackOrder
				.Where(track => !GetIsLocalTrack(track))
				.Select(GetUriFromTrack).ToList();
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
				OffsetParam = new PlayerResumePlaybackRequest.Offset { Uri = useUri ? uriToPlay : trackUris.First() },
				PositionMs = useUri ? positionMs : 0
			};
			await Spotify.Player.ResumePlayback(playbackRequest);
			await Spotify.Player.SetShuffle(new PlayerShuffleRequest(false));
		}
	}
}
