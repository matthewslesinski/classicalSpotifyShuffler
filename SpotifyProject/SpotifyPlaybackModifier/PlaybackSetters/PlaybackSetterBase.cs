using System;
using System.Threading.Tasks;
using ApplicationResources.ApplicationUtils.Parameters;
using CustomResources.Utils.Extensions;
using SpotifyAPI.Web;
using SpotifyProject.Configuration;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackSetters
{
	public abstract class PlaybackSetterBase<ContextT> : SpotifyAccessorBase, IContextSetter<ContextT, IPlaybackSetterArgs> where ContextT : ISpotifyPlaybackContext
	{
		public PlaybackSetterBase(SpotifyConfiguration spotifyConfiguration) : base(spotifyConfiguration)
		{ }

		public async Task SetContext(ContextT context, IPlaybackSetterArgs args)
		{
			PlaybackStateArgs playbackArgs;
			var maintainCurrentListening = TaskParameters.Get<bool>(SpotifyParameters.MaintainCurrentlyPlaying);
			var currentMs = Environment.TickCount;
			var currentlyPlaying = await this.GetCurrentlyPlaying().WithoutContextCapture();
			var elapsedMs = Environment.TickCount - currentMs;
			if (maintainCurrentListening && currentlyPlaying?.Item is FullTrack currentlyPlayingTrack)
			{
				var uriToSetPlayTo = currentlyPlayingTrack.Uri;
				var positionToPlayAtMs = currentlyPlaying.ProgressMs.HasValue ? (currentlyPlaying.ProgressMs.Value + elapsedMs) : (int?)null;
				playbackArgs = new PlaybackStateArgs { UriToPlay = uriToSetPlayTo, PositionToPlayMs = positionToPlayAtMs };
			}
			else
				playbackArgs = new PlaybackStateArgs();
			playbackArgs.CurrentPlaybackFound = currentlyPlaying?.Item != null;
			playbackArgs.AllowUsingContextUri = args.AllowUsingContextUri;
			await SetPlayback(context, playbackArgs).WithoutContextCapture();
		}

		protected abstract Task SetPlayback(ContextT context, IPlaybackStateArgs args);
	}
}
