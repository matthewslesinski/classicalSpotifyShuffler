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
using SpotifyProject.Utils;
using System.Net.Http;
using System.IO;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackSetters
{
	public class PlaylistSetter<TrackT> : SpotifyAccessorBase, IContextSetter<ISpotifyPlaybackContext<TrackT>, IContextSetterArgs>
	{
		public PlaylistSetter(SpotifyConfiguration spotifyConfiguration, IContextSetter<ISpotifyPlaybackContext<TrackT>, IPlaybackSetterArgs> underlyingPlaybackSetter)
			: this(spotifyConfiguration, underlyingPlaybackSetter, new EfficientPlaylistTrackModifier(spotifyConfiguration))
		{ }

		public PlaylistSetter(SpotifyConfiguration spotifyConfiguration, IContextSetter<ISpotifyPlaybackContext<TrackT>, IPlaybackSetterArgs> underlyingPlaybackSetter,
			IPlaylistTrackModifier trackModifier)
			: base(spotifyConfiguration)
		{
			_underlyingPlaybackSetter = underlyingPlaybackSetter;
			_playlistTrackModifier = trackModifier;
		}

		public string PlaylistName => TaskParameters.Get<string>(SpotifyParameters.SaveAsPlaylistName);

		public async Task SetContext(ISpotifyPlaybackContext<TrackT> context, IContextSetterArgs args)
		{
			var foundName = PlaylistName;
			var name = string.IsNullOrWhiteSpace(foundName)
				? UserInterface.Instance.RequestResponseAsync("Please provide a playlist name to save to")
				: Task.FromResult(foundName);
			var reorderedPlaylist = await SaveContextAsPlaylist(context, name).WithoutContextCapture();
			if (_underlyingPlaybackSetter != null)
			{
				var playbackArgs = new PlaybackStateArgs { AllowUsingContextUri = true };
				await _underlyingPlaybackSetter.SetContext(reorderedPlaylist, playbackArgs).WithoutContextCapture();
			}
		}

		private async Task<IPlaylistPlaybackContext<TrackT>> SaveContextAsPlaylist(ISpotifyPlaybackContext<TrackT> context, Task<string> playlistName)
		{
			var playlistObject = await this.AddOrGetPlaylistByName(playlistName).WithoutContextCapture();
			var existingPlaylistContext = new ExistingPlaylistPlaybackContext(SpotifyConfiguration, playlistObject);
			await existingPlaylistContext.FullyLoad().WithoutContextCapture();
			Logger.Information($"Saving new track list to playlist {playlistName.Result}");
			// Set number of retries to 1 because empirically there are a lot of errors that occur
			using (TaskParameters.GetBuilder().With(SpotifyParameters.NumberOfRetriesForServerError, 1).Apply())
			{
				await _playlistTrackModifier.ModifyPlaylistTracks(existingPlaylistContext, context.PlaybackOrder.Select(context.GetMetadataForTrack)).WithoutContextCapture();
			}
			return new ConstructedPlaylistContext<TrackT, ISpotifyPlaybackContext<TrackT>>(context, existingPlaylistContext);
		}

		private readonly IContextSetter<ISpotifyPlaybackContext<TrackT>, PlaybackStateArgs> _underlyingPlaybackSetter;
		private readonly IPlaylistTrackModifier _playlistTrackModifier;
	}

	public interface IPlaylistTrackModifier
	{
		public async Task ModifyPlaylistTracks(ExistingPlaylistPlaybackContext currentVersion, IEnumerable<IPlayableTrackLinkingInfo> newTracks)
		{
			var playlistId = currentVersion.SpotifyContext.Id;
			await SendOperations(playlistId, currentVersion.PlaybackOrder.Select(currentVersion.GetMetadataForTrack).Select(track => track.GetOriginallyRequestedVersion()),
				newTracks.Select(track => track.GetOriginallyRequestedVersion())).WithoutContextCapture();
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
			catch (APIException e) when ((e.Response != null && SpotifyConstants.NonDeterministicStatusCodes.Contains(e.Response.StatusCode) && e.Response.StatusCode != System.Net.HttpStatusCode.ServiceUnavailable)
											|| e.InnerException is HttpRequestException httpE && httpE.InnerException is IOException)
			{
				Logger.Error("An error occurred on the server's end while trying to perform the {modificationType} operation on a playlist. " +
					"The exception has been caught and the operation may be retried. The operation occurred while editing playlist with id {playlistId}," +
					"with provided snapshotId {snapshotId}: {exception}", GetType().Name, playlistId, previousSnapshotId, e);
				return (false, null);
			}
		}
	}
}
