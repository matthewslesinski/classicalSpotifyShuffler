using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using System.Linq;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;
using CustomResources.Utils.Extensions;
using ApplicationResources.Logging;
using CustomResources.Utils.Concepts.DataStructures;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;

namespace SpotifyProject.SpotifyPlaybackModifier
{
	public interface ISpotifyAccessor : ISpotifyConfigurationContainer
	{
	}

	public interface ISpotifyConfigurationContainer
	{
		SpotifyConfiguration SpotifyConfiguration { get; }
		SpotifyClient Spotify => SpotifyConfiguration.Spotify;
	}

	public static class SpotifyConfigurationExtension
	{
		public static Task<CurrentlyPlaying> GetCurrentlyPlaying(this ISpotifyConfigurationContainer spotifyConfigurationContainer) =>
			spotifyConfigurationContainer.Spotify.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest { Market = spotifyConfigurationContainer.SpotifyConfiguration.Market });

		public static async Task<List<FullTrack>> GetAllSavedTracks(this ISpotifyConfigurationContainer spotifyConfigurationContainer, int batchSize = 50)
		{
			var allItems = spotifyConfigurationContainer.Spotify.Paginate(await spotifyConfigurationContainer.Spotify.Library.GetTracks(new LibraryTracksRequest { Limit = batchSize, Market = spotifyConfigurationContainer.SpotifyConfiguration.Market }).WithoutContextCapture());
			var allTracks = await allItems.Select(track => track.Track).ToListAsync().WithoutContextCapture();
			// This is a hack to account for the fact that local tracks are missing data
			allTracks.Where(track => track.IsLocal).Each((track, index) => { if (track.TrackNumber == 0) track.TrackNumber = index; });
			return allTracks;
		}

		public static async Task<List<FullTrack>> GetAllRemainingPlaylistTracks(this ISpotifyConfigurationContainer spotifyConfigurationContainer, string playlistId, int batchSize = 100, int startFrom = 0)
		{
			var allItems = spotifyConfigurationContainer.Spotify.Paginate(await spotifyConfigurationContainer.Spotify.Playlists.GetItems(playlistId, new PlaylistGetItemsRequest { Limit = batchSize, Offset = startFrom, Market = spotifyConfigurationContainer.SpotifyConfiguration.Market }).WithoutContextCapture());
			var allTracks = await allItems.Select(track => track.Track).OfType<FullTrack>().ToListAsync().WithoutContextCapture();
			// This is a hack to account for the fact that local tracks are missing data
			allTracks.Where(track => track.IsLocal).Each((track, index) => { if (track.TrackNumber == 0) track.TrackNumber = index; });
			return allTracks;
		}

		public static async Task<FullAlbum> GetAlbum(this ISpotifyConfigurationContainer spotifyConfigurationContainer, string albumId) =>
			await spotifyConfigurationContainer.Spotify.Albums.Get(albumId, new AlbumRequest { Market = spotifyConfigurationContainer.SpotifyConfiguration.Market }).WithoutContextCapture();

		public static async Task<IList<SimpleTrack>> GetAllAlbumTracks(this ISpotifyConfigurationContainer spotifyConfigurationContainer, string albumId, int batchSize = 50) =>
			await spotifyConfigurationContainer.Spotify.PaginateAll(await spotifyConfigurationContainer.Spotify.Albums.GetTracks(albumId, new AlbumTracksRequest { Limit = batchSize, Market = spotifyConfigurationContainer.SpotifyConfiguration.Market }).WithoutContextCapture());

		public static async Task<FullArtist> GetArtist(this ISpotifyConfigurationContainer spotifyConfigurationContainer, string artistId) =>
			await spotifyConfigurationContainer.Spotify.Artists.Get(artistId).WithoutContextCapture();

		public static async Task<List<SimpleTrackAndAlbumWrapper>> GetAllArtistTracks(this ISpotifyConfigurationContainer spotifyConfigurationContainer, string artistId, ArtistsAlbumsRequest.IncludeGroups albumGroupsToInclude,
			int albumBatchSize = 50, int trackBatchSize = 50)
		{
			var artistsAlbumsRequest = new ArtistsAlbumsRequest { IncludeGroupsParam = albumGroupsToInclude, Limit = albumBatchSize, Market = spotifyConfigurationContainer.SpotifyConfiguration.Market };
			var albumEquality = IAlbumPlaybackContext.SimpleAlbumEqualityComparer;
			var seenAlbums = new InternalConcurrentSet<SimpleAlbum>(albumEquality);
			var firstAlbumPage = await spotifyConfigurationContainer.Spotify.Artists.GetAlbums(artistId, artistsAlbumsRequest).WithoutContextCapture();
			var allAlbumTasks = spotifyConfigurationContainer.Spotify.PaginateConcurrently<SimpleAlbum, Paging<SimpleAlbum>>(firstAlbumPage);
			var allAlbumsLoadedTask = Task.WhenAll(allAlbumTasks).ContinueWith(task => { if (task.IsCompletedSuccessfully) Logger.Information($"All albums loaded"); });
			var allTrackTasks = allAlbumTasks.Select(async albumTask =>
			{
				var album = await albumTask.WithoutContextCapture();
				return !seenAlbums.Add(album)
					? Array.Empty<SimpleTrackAndAlbumWrapper>()
					: (await spotifyConfigurationContainer.GetAllAlbumTracks(album.Id, trackBatchSize).WithoutContextCapture())
                        .Where(track => track.Artists.Select(artist => artist.Id).Contains(artistId)).Select(track => new SimpleTrackAndAlbumWrapper(track, album));
			}).ToList();
			await allAlbumsLoadedTask.WithoutContextCapture();
			var allTracksLoadedTask = Task.WhenAll(allTrackTasks).ContinueWith(task => { if (task.IsCompletedSuccessfully) Logger.Information($"All tracks loaded"); });
			await allTracksLoadedTask.WithoutContextCapture();
			var allTracks = await allTrackTasks.MakeAsync().ToListAsync().WithoutContextCapture();
			return allTracks.SelectMany(tracks => tracks).ToList();
		}

		public static Task SetCurrentPlayback(this ISpotifyConfigurationContainer spotifyConfigurationContainer, PlayerResumePlaybackRequest request) => spotifyConfigurationContainer.Spotify.Player.ResumePlayback(request);

		public static Task SetShuffle(this ISpotifyConfigurationContainer spotifyConfigurationContainer, bool turnShuffleOn) =>
			spotifyConfigurationContainer.Spotify.Player.SetShuffle(new PlayerShuffleRequest(turnShuffleOn));

		public static Task<SnapshotResponse> AddPlaylistItems(this ISpotifyConfigurationContainer spotifyConfigurationContainer, string playlistId, IEnumerable<string> urisToAdd, int? addPosition = null) =>
			spotifyConfigurationContainer.Spotify.Playlists.AddItems(playlistId, new PlaylistAddItemsRequest(urisToAdd is IList<string> urisList ? urisList : urisToAdd.ToList()) { Position = addPosition });

		public static Task<SnapshotResponse> RemovePlaylistItems(this ISpotifyConfigurationContainer spotifyConfigurationContainer, string playlistId, string previousSnapshotId, IEnumerable<string> urisToRemove) =>
			spotifyConfigurationContainer.Spotify.Playlists.RemoveItems(playlistId,
				new PlaylistRemoveItemsRequest { Tracks = urisToRemove.Select(uri => new PlaylistRemoveItemsRequest.Item { Uri = uri }).ToList(), SnapshotId = previousSnapshotId });

		public static Task<SnapshotResponse> ReorderPlaylistItems(this ISpotifyConfigurationContainer spotifyConfigurationContainer, string playlistId, string previousSnapshotId, int rangeStart, int rangeLength, int target) =>
			spotifyConfigurationContainer.Spotify.Playlists.ReorderItems(playlistId, new PlaylistReorderItemsRequest(rangeStart, target) { RangeLength = rangeLength, SnapshotId = previousSnapshotId });

		public static Task<bool> ReplacePlaylistItems(this ISpotifyConfigurationContainer spotifyConfigurationContainer, string playlistId, List<string> urisToReplaceWith = null) =>
			spotifyConfigurationContainer.Spotify.Playlists.ReplaceItems(playlistId, new PlaylistReplaceItemsRequest(urisToReplaceWith ?? new List<string>()));

		public static Task<FullPlaylist> GetPlaylist(this ISpotifyConfigurationContainer spotifyConfigurationContainer, string playlistId, IEnumerable<string> fieldConstraints = null)
		{
			if (fieldConstraints != null)
			{
				var request = new PlaylistGetRequest();
				fieldConstraints.Each(request.Fields.Add);
				return spotifyConfigurationContainer.Spotify.Playlists.Get(playlistId, request);
			}
			return spotifyConfigurationContainer.Spotify.Playlists.Get(playlistId);
		}

		public static Task<PrivateUser> GetCurrentUserProfile(this ISpotifyConfigurationContainer spotifyConfigurationContainer) => spotifyConfigurationContainer.Spotify.UserProfile.Current();

		public static Task<FullPlaylist> AddOrGetPlaylistByName(this ISpotifyConfigurationContainer spotifyConfigurationContainer, string name) => spotifyConfigurationContainer.AddOrGetPlaylistByName(Task.FromResult(name));
		public static async Task<FullPlaylist> AddOrGetPlaylistByName(this ISpotifyConfigurationContainer spotifyConfigurationContainer, Task<string> nameProvider)
		{
			var playlists = spotifyConfigurationContainer.Spotify.Paginate(await spotifyConfigurationContainer.Spotify.Playlists.CurrentUsers(new PlaylistCurrentUsersRequest { Limit = 50 }).WithoutContextCapture());
			var name = await nameProvider.WithoutContextCapture();
			var existingPlaylist = await playlists.FirstOrDefaultAsync(playlist => string.Equals(playlist.Name, name, StringComparison.OrdinalIgnoreCase)).WithoutContextCapture();
			if (existingPlaylist != default)
				return await spotifyConfigurationContainer.GetPlaylist(existingPlaylist.Id).WithoutContextCapture();
			else
			{
				var userProfile = await spotifyConfigurationContainer.GetCurrentUserProfile().WithoutContextCapture();
				var userId = userProfile.Id;
				return await spotifyConfigurationContainer.Spotify.Playlists.Create(userId, new PlaylistCreateRequest(name) { Public = false, Collaborative = false }).WithoutContextCapture();
			}
		}
	}
}
