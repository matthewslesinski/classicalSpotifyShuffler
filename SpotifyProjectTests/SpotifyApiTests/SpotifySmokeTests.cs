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
					var initialOrder = (await SpotifyAccessor.GetAllPlaylistTracks(playlistId)).Select(fullTrack => (fullTrack.Uri, fullTrack.Name)).ToList();
					CollectionAssert.AreEqual(albumOrder, initialOrder);
				}

				var reverseSuccess = await commandExecutor.ModifyContext(PlaybackContextType.Playlist, playlistId).WithoutContextCapture();
				Assert.IsTrue(reverseSuccess);
				var newOrder = (await SpotifyAccessor.GetAllPlaylistTracks(playlistId)).Select(fullTrack => (fullTrack.Uri, fullTrack.Name)).ToList();
				CollectionAssert.AreEqual(albumOrder.Reversed(), newOrder);
			}
		}
	}
}
