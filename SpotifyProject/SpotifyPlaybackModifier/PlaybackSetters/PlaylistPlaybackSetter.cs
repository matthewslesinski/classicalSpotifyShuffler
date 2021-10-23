using System;
using System.Threading.Tasks;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using System.Linq;
using System.Collections.Generic;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;
using ApplicationResources.Setup;
using CustomResources.Utils.Extensions;
using ApplicationResources.Logging;
using ApplicationResources.ApplicationUtils;
using SpotifyProject.Configuration;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackSetters
{
	public class PlaylistPlaybackSetter<TrackT> : SpotifyAccessorBase, IPlaybackSetter<ISpotifyPlaybackContext<TrackT>, PlaybackStateArgs>
	{
		public PlaylistPlaybackSetter(SpotifyConfiguration spotifyConfiguration, IPlaybackSetter<ISpotifyPlaybackContext<TrackT>, PlaybackStateArgs> underlyingPlaybackSetter)
			: this(spotifyConfiguration, underlyingPlaybackSetter, new EfficientPlaylistTrackModifier(spotifyConfiguration))
		{ }

		public PlaylistPlaybackSetter(SpotifyConfiguration spotifyConfiguration, IPlaybackSetter<ISpotifyPlaybackContext<TrackT>, PlaybackStateArgs> underlyingPlaybackSetter,
			IPlaylistTrackModifier trackModifier)
			: base(spotifyConfiguration)
		{
			_underlyingPlaybackSetter = underlyingPlaybackSetter;
			_playlistTrackModifier = trackModifier;
		}

		public string PlaylistName => Settings.Get<string>(SpotifySettings.SaveAsPlaylistName);

		public async Task SetPlayback(ISpotifyPlaybackContext<TrackT> context, PlaybackStateArgs args)
		{
			var name = PlaylistName;
			if (string.IsNullOrWhiteSpace(name))
				name = await UserInterface.Instance.RequestResponseAsync("Please provide a playlist name to save to").WithoutContextCapture();
			var reorderedPlaylist = await SaveContextAsPlaylist(context, name).WithoutContextCapture();
			args.AllowUsingContextUri = true;
			await _underlyingPlaybackSetter.SetPlayback(reorderedPlaylist, args).WithoutContextCapture();
		}

		private async Task<IPlaylistPlaybackContext<TrackT>> SaveContextAsPlaylist(ISpotifyPlaybackContext<TrackT> context, string playlistName)
		{
			var playlistObject = await this.AddOrGetPlaylistByName(playlistName).WithoutContextCapture();
			var existingPlaylistContext = new ExistingPlaylistPlaybackContext(SpotifyConfiguration, playlistObject);
			await existingPlaylistContext.FullyLoad().WithoutContextCapture();
			Logger.Information($"Saving new track list to playlist {playlistName}");
			await _playlistTrackModifier.ModifyPlaylistTracks(existingPlaylistContext, context.PlaybackOrder.Select(context.GetMetadataForTrack)).WithoutContextCapture();
			return new ConstructedPlaylistContext<TrackT, ISpotifyPlaybackContext<TrackT>>(context, existingPlaylistContext);
		}

		private readonly IPlaybackSetter<ISpotifyPlaybackContext<TrackT>, PlaybackStateArgs> _underlyingPlaybackSetter;
		private readonly IPlaylistTrackModifier _playlistTrackModifier;
	}

	public interface IPlaylistTrackModifier
	{
		public async Task ModifyPlaylistTracks(ExistingPlaylistPlaybackContext currentVersion, IEnumerable<ITrackLinkingInfo> newTracks)
		{
			var playlistId = currentVersion.SpotifyContext.Id;
			await SendOperations(playlistId, currentVersion.PlaybackOrder.Select(currentVersion.GetMetadataForTrack), newTracks).WithoutContextCapture();
		}

		protected Task SendOperations(string playlistId, IEnumerable<ITrackLinkingInfo> currentTracks, IEnumerable<ITrackLinkingInfo> newTracks);
	}

	public interface IPlaylistModification
	{
		Task<string> SendRequest(ISpotifyAccessor spotifyAccessor, string playlistId, string previousSnapshotId = null);
	}
}
