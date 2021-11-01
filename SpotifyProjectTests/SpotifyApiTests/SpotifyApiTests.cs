using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using SpotifyAPI.Web;
using SpotifyProject.Setup;
using SpotifyProject.SpotifyPlaybackModifier;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackSetters;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;
using SpotifyProject.Utils;

namespace SpotifyProjectTests.SpotifyApiTests
{
	public class SpotifyApiTests : SpotifyTestBase
	{

		[Test]
		public async Task CanConnectToSpotify()
		{
			var userProfile = await SpotifyAccessor.GetCurrentUserProfile();
			Assert.IsNotNull(userProfile.DisplayName);
		}

		[Test]
		public async Task RateLimitTest()
		{
			var albumId = SpotifyDependentUtils.TryParseSpotifyUri(SampleAlbumUris[SampleAlbums.BeethovenPianoSonatasAndConcerti], out _, out var parsedId, out _) ? parsedId : null;
			var otherAlbumId = SpotifyDependentUtils.TryParseSpotifyUri(SampleAlbumUris[SampleAlbums.BachKeyboardWorks], out _, out var parsedOtherId, out _) ? parsedOtherId : null;

			var tasks = Enumerable.Range(0, 1000).Select(_ => SpotifyAccessor.GetAlbum(albumId));
			await SpotifyAccessor.GetAlbum(otherAlbumId);
			await Task.WhenAll(tasks);
			Assert.Pass();
		}

		[Test]
		public async Task TestAlbumGetTracks()
		{
			var albumId = SpotifyDependentUtils.TryParseSpotifyUri(SampleAlbumUris[SampleAlbums.BeethovenPianoSonatasAndConcerti], out _, out var parsedId, out _) ? parsedId : null;
			var beethovenPianoSonatasTracks = await SpotifyAccessor.GetAllAlbumTracks(albumId, batchSize: 1);
			var beethovenPianoSonatasAlbum = await SpotifyAccessor.GetAlbum(albumId);
			var beethovenPianoSonatasTrackInfos = beethovenPianoSonatasTracks.Select(track => new SimpleTrackAndAlbumWrapper(track, beethovenPianoSonatasAlbum));
			var sortedTracks = beethovenPianoSonatasTrackInfos.OrderBy(ITrackLinkingInfo.TrackOrderWithinAlbums);
			CollectionAssert.AreEqual(beethovenPianoSonatasTrackInfos.Select<ITrackLinkingInfo, string>(track => track.Name), sortedTracks.Select(track => track.Name), "The beethoven sonatas were returned in the wrong order. " +
				$"The expected order was: \n{TurnTracksIntoString(sortedTracks)}\n but the retrieved order was \n {TurnTracksIntoString(beethovenPianoSonatasTrackInfos)}");
		}

		[Test]
		public async Task TestPlaylistAddingTracks()
		{
			var playlist = await SpotifyAccessor.AddOrGetPlaylistByName(GetPlaylistNameForTest(nameof(TestPlaylistAddingTracks)));
			await SpotifyAccessor.ReplacePlaylistItems(playlist.Id);
			SpotifyDependentUtils.TryParseSpotifyUri(SampleArtistUris[SampleArtists.YannickNezetSeguin], out _, out var yannickId, out _);
			var yannickTracks = (await SpotifyAccessor.GetAllArtistTracks(yannickId, SpotifyAPI.Web.ArtistsAlbumsRequest.IncludeGroups.Album)).ToArray();
			var yannickTrackUris = yannickTracks.Select(track => track.OriginalTrack.Uri);
			var yannickUrisToTracks = yannickTracks.GroupBy(track => track.OriginalTrack.Uri).ToDictionary(group => group.Key, group => group.First());
			var trackBatches = yannickTrackUris.Batch(SpotifyConstants.PlaylistRequestBatchSize);
			var addTracksTasks = trackBatches.Select(batch => SpotifyAccessor.AddPlaylistItems(playlist.Id, batch)).ToList();
			foreach (var task in addTracksTasks)
				await task;

			var playlistTracks = (await SpotifyAccessor.GetAllPlaylistTracks(playlist.Id)).ToArray();
			var playlistUrisToTracks = playlistTracks.GroupBy(track => track.Uri).ToDictionary(group => group.Key, group => group.First());

			var playlistUris = playlistTracks.Select(track => track.Uri).ToList();
			var playlistUrisIndices = playlistTracks.Select(track => track.Uri).ToIndexMap();
			Assert.IsTrue(yannickTrackUris.ContainsSameElements(playlistUris, out var diffs),
				"The following tracks were not correctly added to the playlist: " +
				string.Join(", ", diffs.Select(diff => (diff.element, yannickUrisToTracks.TryGetValue(diff.element, out var track)
																		? track.OriginalTrack.Name
																		: playlistUrisToTracks[diff.element].Name, diff.sequence1Count, diff.sequence2Count))
										.Select(diff => $"({diff.element}, {diff.Item2}, {diff.sequence1Count}, {diff.sequence2Count})")));
			foreach(var (batch, batchIndex) in trackBatches.Enumerate())
			{
				var contains = playlistUrisIndices.TryGetValue(batch.First(), out var indices);

				var potentialMatches = indices.Select(firstIndex => playlistUris.GetRange(firstIndex, Math.Min(firstIndex + batch.Count, playlistUris.Count) - firstIndex));

				Assert.That(potentialMatches.Any(match => match.SequenceEqual(batch)),
					$"Batch number {batchIndex + 1} with length {batch.Count} was not added to the playlist in sequential order. It contained tracks: \n" +
					TurnTracksIntoString(batch.Select(uri => yannickUrisToTracks[uri])) +
					$"\n But the playlist match ended up being {(potentialMatches.Count() > 1 ? "one of the following sequences" : "the following sequence")}:\n" +
					string.Join("\n--------------------------------------\n",
						potentialMatches.Select(match => TurnUrisIntoString(match, uri => playlistUrisToTracks[uri].Name,
						uri => (playlistUrisToTracks[uri].DiscNumber, playlistUrisToTracks[uri].TrackNumber),
						uri => playlistUrisToTracks[uri].Album.Name))));
			}

		}

		[TestCase("BrahmsSymphonies", 0, "reversed order by album index")]
		[TestCase("BrahmsSymphonies", 1, "order by album index")]
		public async Task TestReorderingPlaylist(string albumToUse, int testCaseIndex, string testCaseDescriptor)
		{
			var albumEnum = Enum.Parse<SampleAlbums>(albumToUse, true);
			var testCaseOrdering = TestCasesForTestReorderingPlaylist[testCaseIndex];
			var playlist = await SpotifyAccessor.AddOrGetPlaylistByName(GetPlaylistNameForTest(nameof(TestReorderingPlaylist)));
			await SpotifyAccessor.ReplacePlaylistItems(playlist.Id);
			var albumId = SpotifyDependentUtils.TryParseSpotifyUri(SampleAlbumUris[albumEnum], out _, out var parsedId, out _) ? parsedId : null;
			var tracks = await SpotifyAccessor.GetAllAlbumTracks(albumId);
			var album = await SpotifyAccessor.GetAlbum(albumId);
			var trackInfos = tracks.Select<SimpleTrack, ITrackLinkingInfo>(track => new SimpleTrackAndAlbumWrapper(track, album));
			var sortedTrackInfos = trackInfos.OrderBy(testCaseOrdering);
			await SpotifyAccessor.AddPlaylistItems(playlist.Id, sortedTrackInfos.Select(track => track.Uri));
			var existingPlaylistContext = new ExistingPlaylistPlaybackContext(SpotifyAccessor.SpotifyConfiguration, playlist);
			await existingPlaylistContext.FullyLoad();
			CollectionAssert.AreEqual(sortedTrackInfos.Select(track => track.Uri), existingPlaylistContext.PlaybackOrder.Select(track => track.Uri));
			IPlaylistTrackModifier playlistModifier = new EfficientPlaylistTrackModifier(SpotifyAccessor.SpotifyConfiguration);
			await playlistModifier.ModifyPlaylistTracks(existingPlaylistContext, trackInfos);
			var newTracks = await SpotifyAccessor.GetAllPlaylistTracks(playlist.Id);
			CollectionAssert.AreEqual(trackInfos.Select(track => track.Uri), newTracks.Select(track => track.Uri), "The playlist resulted in the wrong order. " +
				$"The expected order was: \n{TurnTracksIntoString(trackInfos)}\n but the retrieved order was \n {TurnTracksIntoString(newTracks.Select(existingPlaylistContext.GetMetadataForTrack))}");
		}

		private static readonly IComparer<ITrackLinkingInfo>[] TestCasesForTestReorderingPlaylist = new[] {
			ITrackLinkingInfo.TrackOrderWithinAlbums.Reversed(),
			ITrackLinkingInfo.TrackOrderWithinAlbums,
		};


		private static string TurnTracksIntoString(IEnumerable<ITrackLinkingInfo> tracks) => TurnUrisIntoString(tracks.Select(track => (track.Uri, track.AlbumName, track.AlbumIndex.discNumber, track.AlbumIndex.trackNumber, track.Name)));

		private static string TurnUrisIntoString(IEnumerable<string> uris, Func<string, string> trackNameGetter, Func<string, (int discNumber, int trackNumber)> albumIndexGetter, Func<string, string> albumNameGetter) =>
			TurnUrisIntoString(uris.Zip(uris.Select(albumNameGetter), uris.Select(uri => albumIndexGetter(uri).discNumber), uris.Select(uri => albumIndexGetter(uri).trackNumber), uris.Select(trackNameGetter)));

		private static string TurnUrisIntoString(IEnumerable<(string uri, string albumName, int discNumber, int trackNumber, string trackName)> trackInfos) =>
			$"{string.Join("\n", trackInfos.Select(info => $"\t{info.ToDescriptiveString()}"))}";

	}
}
