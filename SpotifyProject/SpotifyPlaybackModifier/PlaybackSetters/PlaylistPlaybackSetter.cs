using System;
using System.Threading.Tasks;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using System.Linq;
using System.Collections.Generic;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;
using CustomResources.Utils.Extensions;
using ApplicationResources.Logging;
using ApplicationResources.ApplicationUtils;
using SpotifyProject.Configuration;
using ApplicationResources.ApplicationUtils.Parameters;
using SpotifyAPI.Web;

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

		public string PlaylistName => TaskParameters.Get<string>(SpotifyParameters.SaveAsPlaylistName);

		public async Task SetPlayback(ISpotifyPlaybackContext<TrackT> context, PlaybackStateArgs args)
		{
			var name = PlaylistName;
			if (string.IsNullOrWhiteSpace(name))
				name = await UserInterface.Instance.RequestResponseAsync("Please provide a playlist name to save to").WithoutContextCapture();
			var reorderedPlaylist = await SaveContextAsPlaylist(context, name).WithoutContextCapture();
			if (_underlyingPlaybackSetter != null)
			{
				args.AllowUsingContextUri = true;
				await _underlyingPlaybackSetter.SetPlayback(reorderedPlaylist, args).WithoutContextCapture();
			}
		}

		private async Task<IPlaylistPlaybackContext<TrackT>> SaveContextAsPlaylist(ISpotifyPlaybackContext<TrackT> context, string playlistName)
		{
			var playlistObject = await this.AddOrGetPlaylistByName(playlistName).WithoutContextCapture();
			var existingPlaylistContext = new ExistingPlaylistPlaybackContext(SpotifyConfiguration, playlistObject);
			await existingPlaylistContext.FullyLoad().WithoutContextCapture();
			Logger.Information($"Saving new track list to playlist {playlistName}");
			// Set number of retries to 1 because empirically there are a lot of errors that occur
			using (TaskParameters.GetBuilder().With(SpotifyParameters.NumberOfRetriesForServerError, 1).Apply())
			{
				await _playlistTrackModifier.ModifyPlaylistTracks(existingPlaylistContext, context.PlaybackOrder.Select(context.GetMetadataForTrack)).WithoutContextCapture();
			}
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

		async Task<(bool ranSuccessfuly, string resultingSnapshotId)> TrySendRequest(ISpotifyAccessor spotifyAccessor, string playlistId, string previousSnapshotId = null)
		{
			try
			{
				return (true, await SendRequest(spotifyAccessor, playlistId, previousSnapshotId));
			}
			catch (APIException e)
			{
				Logger.Error("An error occurred on the server's end while trying to perform the {modificationType} operation on a playlist. " +
					"The exception has been caught and the operation may be retried. The operation occurred while editing playlist with id {playlistId}," +
					"with provided snapshotId {snapshotId}: {exception}", GetType().Name, playlistId, previousSnapshotId, e);
				return (false, null);
			}
		}
	}
}
