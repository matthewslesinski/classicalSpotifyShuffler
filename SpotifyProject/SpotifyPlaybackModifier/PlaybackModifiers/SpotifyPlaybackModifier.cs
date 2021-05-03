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
			Transformer = transformation;
			_trackUriAccessor = trackUriAccessor;
			_trackIsLocalAccessor = trackIsLocalAccessor;
		}

		public TransformT Transformer { get; }

		protected virtual string GetUriFromTrack(TrackT track) => _trackUriAccessor(track);

		protected virtual bool GetIsLocalTrack(TrackT track) {
			var isLocal = _trackIsLocalAccessor(track);
			if (isLocal)
				Logger.Warning($"Not including track {GetUriFromTrack(track)} because it is a local track and spotify doesn't support them through their API");
			return isLocal;
		}

		public async Task SetCurrentPlaybackContext(OutputContextT context, string uriToPlay = null, int positionMs = 0)
		{
			Logger.Information($"Setting {context.PlaybackOrder.Count()} tracks");
			await Spotify.Player.SetShuffle(new PlayerShuffleRequest(false));
			var trackUris = context.PlaybackOrder
				.Where(track => !GetIsLocalTrack(track))
				.Select(GetUriFromTrack).Take(GlobalCommandLine.Store.GetOptionValue<int>(CommandLineOptions.Names.TrackQueueSizeLimit))
				.ToList();
			var useUri = uriToPlay != null && trackUris.Contains(uriToPlay);
			var playbackRequest = new PlayerResumePlaybackRequest
			{
				Uris = trackUris,
				OffsetParam = new PlayerResumePlaybackRequest.Offset { Uri = useUri ? uriToPlay : trackUris.First() },
				PositionMs = useUri ? positionMs : 0
			};
			await Spotify.Player.ResumePlayback(playbackRequest);
		}
	}
}
