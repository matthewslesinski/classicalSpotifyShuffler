using System;
using System.Linq;
using System.Threading.Tasks;
using ApplicationResources.ApplicationUtils.Parameters;
using CustomResources.Utils.Extensions;
using NUnit.Framework;
using SpotifyAPI.Web;
using SpotifyProject.Configuration;
using SpotifyProject.SpotifyPlaybackModifier;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackSetters;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;
using SpotifyProject.SpotifyPlaybackModifier.Transformations;

namespace SpotifyProjectTests.SpotifyApiTests
{
	public class SpotifySmokeTests : SpotifyTestBase
	{

		[Test]
		public async Task TestEfficientPlaybackSetter()
		{
			var sampleAlbum = SampleAlbums.ShostakovichQuartets;
			var playlistName = GetPlaylistNameForTest(nameof(TestEfficientPlaybackSetter));
			var paramBuilder = TaskParameters.GetBuilder()
				.With(SpotifyParameters.TransformationName, nameof(IPlaybackTransformationsStore<IOriginalPlaylistPlaybackContext, FullTrack>.ReverseOrder))
				.With(SpotifyParameters.PlaybackSetterName, nameof(SpotifyUpdaters<FullTrack>.EfficientPlaylistSetterWithoutPlayback))
				.With(SpotifyParameters.NumberOfRetriesForServerError, 1)
				.With(SpotifyParameters.SaveAsPlaylistName, playlistName)
				.With(SpotifyParameters.SerializeOperations, false)
				.With(SpotifyParameters.MaximumBatchSizeToReplaceInPlaylist, 1)
				.With(SpotifyParameters.PlaylistRequestBatchSize, 1);
			var setupTransformationParamOverride = TaskParameters.GetBuilder()
				.With(SpotifyParameters.TransformationName, nameof(IPlaybackTransformationsStore<IOriginalPlaylistPlaybackContext, FullTrack>.SameOrder));

			var albumUri = SampleAlbumUris[sampleAlbum];
			var commandExecutor = new SpotifyCommandExecutor(SpotifyAccessor.Spotify);
			var albumOrder = (await SpotifyAccessor.GetAllAlbumTracks(SampleAlbumIds[sampleAlbum]))
				.Select(simpleTrack => (simpleTrack.Uri, simpleTrack.Name)).ToList();

			using (paramBuilder.Apply())
			{
				var playlistId = (await SpotifyAccessor.AddOrGetPlaylistByName(playlistName).WithoutContextCapture()).Id;
				using (setupTransformationParamOverride.Apply())
				{
					var wipeCleanSuccess = await SpotifyAccessor.ReplacePlaylistItems(playlistId);
					var setupSuccess = await commandExecutor.ModifyContext(albumUri).WithoutContextCapture();
					Assert.IsTrue(setupSuccess);
					var initialOrder = (await SpotifyAccessor.GetAllRemainingPlaylistTracks(playlistId)).Select(fullTrack => (fullTrack.Uri, fullTrack.Name)).ToList();
					CollectionAssert.AreEqual(albumOrder, initialOrder);
				}

				var reverseSuccess = await commandExecutor.ModifyContext(PlaybackContextType.Playlist, playlistId).WithoutContextCapture();
				Assert.IsTrue(reverseSuccess);
				var newOrder = (await SpotifyAccessor.GetAllRemainingPlaylistTracks(playlistId).WithoutContextCapture()).Select(fullTrack => (fullTrack.Uri, fullTrack.Name)).ToList();
				CollectionAssert.AreEqual(albumOrder.Reversed(), newOrder);
			}
		}

		[Test]
		public async Task TestLocalFilesInPlaylists()
		{
			var samplePlaylist = SamplePlaylists.ImportedFromYoutube;
			var playlistName = GetPlaylistNameForTest(nameof(TestLocalFilesInPlaylists));
			var paramBuilder = TaskParameters.GetBuilder()
				.With(SpotifyParameters.TransformationName, nameof(IPlaybackTransformationsStore<IOriginalPlaylistPlaybackContext, FullTrack>.ReverseOrder))
				.With(SpotifyParameters.PlaybackSetterName, nameof(SpotifyUpdaters<FullTrack>.EfficientPlaylistSetterWithoutPlayback))
				.With(SpotifyParameters.NumberOfRetriesForServerError, 1)
				.With(SpotifyParameters.SaveAsPlaylistName, playlistName)
				.With(SpotifyParameters.SerializeOperations, false)
				.With(SpotifyParameters.MaximumBatchSizeToReplaceInPlaylist, 1)
				.With(SpotifyParameters.PlaylistRequestBatchSize, 1);

			var playlistUri = SamplePlaylistUris[samplePlaylist];
			var playlistId = SamplePlaylistIds[samplePlaylist];
			var commandExecutor = new SpotifyCommandExecutor(SpotifyAccessor.Spotify);
			var playlistObj = await SpotifyAccessor.AddOrGetPlaylistByName(playlistName);
			if (!Equals(playlistId, playlistObj.Id))
				Assert.Inconclusive($"The required playlist to run this test does not exist, or has a different name/id");
			var playlistOrder = (await SpotifyAccessor.GetAllRemainingPlaylistTracks(SamplePlaylistIds[samplePlaylist])).ToList();
			if (!playlistOrder.Any() || !playlistOrder.All(track => track.IsLocal))
				Assert.Inconclusive($"The playlist with name {playlistName} did not have the required set of tracks to perform this test");

			using (paramBuilder.Apply())
			{
				var reverseSuccess = await commandExecutor.ModifyContext(PlaybackContextType.Playlist, playlistId).WithoutContextCapture();
				Assert.IsTrue(reverseSuccess);
				var newOrder = await SpotifyAccessor.GetAllRemainingPlaylistTracks(playlistId).WithoutContextCapture();
				CollectionAssert.AreEqual(playlistOrder.Select(track => (track.Uri, track.Name)).Reverse(), newOrder.Select(track => (track.Uri, track.Name)));
			}
		}

		[Test]
		public async Task TestSavingAllLikedTracksAsPlaylist()
		{
			var playlistName = GetPlaylistNameForTest(nameof(TestSavingAllLikedTracksAsPlaylist));
			var paramBuilder = TaskParameters.GetBuilder()
				.With(SpotifyParameters.TransformationName, nameof(IPlaybackTransformationsStore<IOriginalAllLikedTracksPlaybackContext, FullTrack>.SameOrder))
				.With(SpotifyParameters.PlaybackSetterName, nameof(SpotifyUpdaters<FullTrack>.EfficientPlaylistSetterWithoutPlayback))
				.With(SpotifyParameters.NumberOfRetriesForServerError, 1)
				.With(SpotifyParameters.SaveAsPlaylistName, playlistName)
				.With(SpotifyParameters.SerializeOperations, false);
			using (paramBuilder.Apply())
			{
				var commandExecutor = new SpotifyCommandExecutor(SpotifyAccessor.Spotify);
				Assert.IsTrue(PlaybackContextConstructors.TryGetExistingContextConstructorForType<IOriginalAllLikedTracksPlaybackContext, FullTrack>(PlaybackContextType.AllLikedTracks, out var constructor));
				var context = await constructor(SpotifyAccessor.SpotifyConfiguration, null).WithoutContextCapture();
				var trackOrder = context.PlaybackOrder.Select(context.GetMetadataForTrack).ToArray();
				var playlistObj = await SpotifyAccessor.AddOrGetPlaylistByName(playlistName).WithoutContextCapture();
				var playlistId = playlistObj.Id;
				await SpotifyAccessor.ReplacePlaylistItems(playlistId);
				var success = await commandExecutor.ModifyContext<IOriginalAllLikedTracksPlaybackContext, FullTrack>(() => Task.FromResult(context), PlaybackContextType.AllLikedTracks, null).WithoutContextCapture();
				Assert.IsTrue(success);
				var newOrder = (await SpotifyAccessor.GetAllRemainingPlaylistTracks(playlistId)).Select(context.GetMetadataForTrack);
				Assert.IsTrue(trackOrder.IsSuperSequenceOf(newOrder, ITrackLinkingInfo.EqualityByUris));
			}
		}
	}
}
