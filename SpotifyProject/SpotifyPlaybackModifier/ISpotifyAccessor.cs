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
using System.Threading;

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

	public static class SpotifyConfigurationExtensions
	{
		public static Task<CurrentlyPlaying> GetCurrentlyPlaying(this ISpotifyConfigurationContainer spotifyConfigurationContainer, CancellationToken cancellationToken = default) =>
			spotifyConfigurationContainer.Spotify.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest { Market = spotifyConfigurationContainer.SpotifyConfiguration.Market }).WaitAsync(cancellationToken);

		public static async Task<List<FullTrack>> GetAllSavedTracks(this ISpotifyConfigurationContainer spotifyConfigurationContainer, int batchSize = 50, CancellationToken cancellationToken = default)
		{
			var allItems = spotifyConfigurationContainer.Spotify.Paginate(await spotifyConfigurationContainer.Spotify.Library.GetTracks(new LibraryTracksRequest { Limit = batchSize, Market = spotifyConfigurationContainer.SpotifyConfiguration.Market }).WithoutContextCapture(), cancellationToken: cancellationToken);
			var allTracks = await allItems.Select(track => track.Track).ToListAsync(cancellationToken: cancellationToken).WithoutContextCapture();
			// This is a hack to account for the fact that local tracks are missing data
			allTracks.Where(track => track.IsLocal).Each((track, index) => { if (track.TrackNumber == 0) track.TrackNumber = index; });
			return allTracks;
		}

		public static async Task<List<FullAlbum>> GetAllSavedAlbums(this ISpotifyConfigurationContainer spotifyConfigurationContainer, int batchSize = 50, CancellationToken cancellationToken = default)
		{
			var initialRequest = await spotifyConfigurationContainer.Spotify.Library.GetAlbums(new LibraryAlbumsRequest { Limit = batchSize, Market = spotifyConfigurationContainer.SpotifyConfiguration.Market }).WaitAsync(cancellationToken).WithoutContextCapture();
			var allAlbumsTask = spotifyConfigurationContainer.Spotify.Paginate(initialRequest, cancellationToken: cancellationToken);
			var allAlbums = await allAlbumsTask.Select(a => a.Album).ToListAsync(cancellationToken: cancellationToken).WithoutContextCapture();
			return allAlbums;
		}

		public static async Task<List<SimplePlaylist>> GetAllSavedPlaylists(this ISpotifyConfigurationContainer spotifyConfigurationContainer, int batchSize = 50, CancellationToken cancellationToken = default)
		{
			var initialRequest = await spotifyConfigurationContainer.Spotify.Playlists.CurrentUsers(new PlaylistCurrentUsersRequest { Limit = batchSize }).WaitAsync(cancellationToken).WithoutContextCapture();
			var allPlaylistsTask = spotifyConfigurationContainer.Spotify.Paginate(initialRequest, cancellationToken: cancellationToken);
			var allPlaylists = await allPlaylistsTask.ToListAsync(cancellationToken: cancellationToken).WithoutContextCapture();
			return allPlaylists;
		}

		public static async Task<List<FullTrack>> GetAllRemainingPlaylistTracks(this ISpotifyConfigurationContainer spotifyConfigurationContainer, string playlistId, int batchSize = 100, int startFrom = 0, CancellationToken cancellationToken = default)
		{
			var allItems = spotifyConfigurationContainer.Spotify.Paginate(await spotifyConfigurationContainer.Spotify.Playlists.GetItems(playlistId, new PlaylistGetItemsRequest { Limit = batchSize, Offset = startFrom, Market = spotifyConfigurationContainer.SpotifyConfiguration.Market }).WaitAsync(cancellationToken).WithoutContextCapture(), cancellationToken: cancellationToken);
			var allTracks = await allItems.Select(track => track.Track).OfType<FullTrack>().ToListAsync(cancellationToken: cancellationToken).WithoutContextCapture();
			// This is a hack to account for the fact that local tracks are missing data
			allTracks.Where(track => track.IsLocal).Each((track, index) => { if (track.TrackNumber == 0) track.TrackNumber = index; });
			return allTracks;
		}

		public static async Task<FullAlbum> GetAlbum(this ISpotifyConfigurationContainer spotifyConfigurationContainer, string albumId, CancellationToken cancellationToken = default) =>
			await spotifyConfigurationContainer.Spotify.Albums.Get(albumId, new AlbumRequest { Market = spotifyConfigurationContainer.SpotifyConfiguration.Market }).WaitAsync(cancellationToken).WithoutContextCapture();

		public static async Task<IList<SimpleTrack>> GetAllAlbumTracks(this ISpotifyConfigurationContainer spotifyConfigurationContainer, string albumId, int batchSize = 50, CancellationToken cancellationToken = default) =>
			await spotifyConfigurationContainer.Spotify.PaginateAll(await spotifyConfigurationContainer.Spotify.Albums.GetTracks(albumId, new AlbumTracksRequest { Limit = batchSize, Market = spotifyConfigurationContainer.SpotifyConfiguration.Market }).WaitAsync(cancellationToken).WithoutContextCapture()).WaitAsync(cancellationToken).WithoutContextCapture();

		public static async Task<FullArtist> GetArtist(this ISpotifyConfigurationContainer spotifyConfigurationContainer, string artistId, CancellationToken cancellationToken = default) =>
			await spotifyConfigurationContainer.Spotify.Artists.Get(artistId).WaitAsync(cancellationToken).WithoutContextCapture();

		public static async Task<List<SimpleTrackAndAlbumWrapper>> GetAllArtistTracks(this ISpotifyConfigurationContainer spotifyConfigurationContainer, string artistId, ArtistsAlbumsRequest.IncludeGroups albumGroupsToInclude,
			int albumBatchSize = 50, int trackBatchSize = 50, CancellationToken cancellationToken = default)
		{
			var artistsAlbumsRequest = new ArtistsAlbumsRequest { IncludeGroupsParam = albumGroupsToInclude, Limit = albumBatchSize, Market = spotifyConfigurationContainer.SpotifyConfiguration.Market };
			var albumEquality = IAlbumPlaybackContext.SimpleAlbumEqualityComparer;
			var seenAlbums = new InternalConcurrentSet<SimpleAlbum>(albumEquality);
			var firstAlbumPage = await spotifyConfigurationContainer.Spotify.Artists.GetAlbums(artistId, artistsAlbumsRequest).WaitAsync(cancellationToken).WithoutContextCapture();
			var allAlbumTasks = spotifyConfigurationContainer.Spotify.PaginateConcurrently<SimpleAlbum, Paging<SimpleAlbum>>(firstAlbumPage).Select(task => task.WaitAsync(cancellationToken));
			var allAlbumsLoadedTask = Task.WhenAll(allAlbumTasks).ContinueWith(task => { if (task.IsCompletedSuccessfully) Logger.Information($"All albums loaded"); }, cancellationToken);
			var allTrackTasks = allAlbumTasks.Select(async albumTask =>
			{
				var album = await albumTask.WaitAsync(cancellationToken).WithoutContextCapture();
				return !seenAlbums.Add(album)
					? Array.Empty<SimpleTrackAndAlbumWrapper>()
					: (await spotifyConfigurationContainer.GetAllAlbumTracks(album.Id, trackBatchSize, cancellationToken).WithoutContextCapture())
                        .Where(track => track.Artists.Select(artist => artist.Id).Contains(artistId)).Select(track => new SimpleTrackAndAlbumWrapper(track, album));
			}).ToList();
			await allAlbumsLoadedTask.WithoutContextCapture();
			var allTracksLoadedTask = Task.WhenAll(allTrackTasks).ContinueWith(task => { if (task.IsCompletedSuccessfully) Logger.Information($"All tracks loaded"); }, cancellationToken);
			await allTracksLoadedTask.WithoutContextCapture();
			var allTracks = await allTrackTasks.MakeAsync(cancel: cancellationToken).ToListAsync(cancellationToken: cancellationToken).WithoutContextCapture();
			return allTracks.SelectMany(tracks => tracks).ToList();
		}

		public static Task SetCurrentPlayback(this ISpotifyConfigurationContainer spotifyConfigurationContainer, PlayerResumePlaybackRequest request, CancellationToken cancellationToken = default) => spotifyConfigurationContainer.Spotify.Player.ResumePlayback(request).WaitAsync(cancellationToken);

		public static Task SetShuffle(this ISpotifyConfigurationContainer spotifyConfigurationContainer, bool turnShuffleOn, CancellationToken cancellationToken = default) =>
			spotifyConfigurationContainer.Spotify.Player.SetShuffle(new PlayerShuffleRequest(turnShuffleOn)).WaitAsync(cancellationToken);

		public static Task<SnapshotResponse> AddPlaylistItems(this ISpotifyConfigurationContainer spotifyConfigurationContainer, string playlistId, IEnumerable<string> urisToAdd, int? addPosition = null, CancellationToken cancellationToken = default) =>
			spotifyConfigurationContainer.Spotify.Playlists.AddItems(playlistId, new PlaylistAddItemsRequest(urisToAdd is IList<string> urisList ? urisList : urisToAdd.ToList()) { Position = addPosition }).WaitAsync(cancellationToken);

		public static Task<SnapshotResponse> RemovePlaylistItems(this ISpotifyConfigurationContainer spotifyConfigurationContainer, string playlistId, string previousSnapshotId, IEnumerable<string> urisToRemove, CancellationToken cancellationToken = default) =>
			spotifyConfigurationContainer.Spotify.Playlists.RemoveItems(playlistId,
				new PlaylistRemoveItemsRequest { Tracks = urisToRemove.Select(uri => new PlaylistRemoveItemsRequest.Item { Uri = uri }).ToList(), SnapshotId = previousSnapshotId }).WaitAsync(cancellationToken);

		public static Task<SnapshotResponse> ReorderPlaylistItems(this ISpotifyConfigurationContainer spotifyConfigurationContainer, string playlistId, string previousSnapshotId, int rangeStart, int rangeLength, int target, CancellationToken cancellationToken = default) =>
			spotifyConfigurationContainer.Spotify.Playlists.ReorderItems(playlistId, new PlaylistReorderItemsRequest(rangeStart, target) { RangeLength = rangeLength, SnapshotId = previousSnapshotId }).WaitAsync(cancellationToken);

		public static Task<bool> ReplacePlaylistItems(this ISpotifyConfigurationContainer spotifyConfigurationContainer, string playlistId, List<string> urisToReplaceWith = null, CancellationToken cancellationToken = default) =>
			spotifyConfigurationContainer.Spotify.Playlists.ReplaceItems(playlistId, new PlaylistReplaceItemsRequest(urisToReplaceWith ?? new List<string>())).WaitAsync(cancellationToken);

		public static Task<FullPlaylist> GetPlaylist(this ISpotifyConfigurationContainer spotifyConfigurationContainer, string playlistId, IEnumerable<string> fieldConstraints = null, CancellationToken cancellationToken = default)
		{
			if (fieldConstraints != null)
			{
				var request = new PlaylistGetRequest();
				fieldConstraints.Each(request.Fields.Add);
				return spotifyConfigurationContainer.Spotify.Playlists.Get(playlistId, request).WaitAsync(cancellationToken);
			}
			return spotifyConfigurationContainer.Spotify.Playlists.Get(playlistId).WaitAsync(cancellationToken);
		}

		public static Task<PrivateUser> GetCurrentUserProfile(this ISpotifyConfigurationContainer spotifyConfigurationContainer, CancellationToken cancellationToken = default) => spotifyConfigurationContainer.Spotify.UserProfile.Current().WaitAsync(cancellationToken);

		public static Task<FullPlaylist> AddOrGetPlaylistByName(this ISpotifyConfigurationContainer spotifyConfigurationContainer, string name, CancellationToken cancellationToken = default) => spotifyConfigurationContainer.AddOrGetPlaylistByName(Task.FromResult(name), cancellationToken: cancellationToken);
		public static async Task<FullPlaylist> AddOrGetPlaylistByName(this ISpotifyConfigurationContainer spotifyConfigurationContainer, Task<string> nameProvider, CancellationToken cancellationToken = default)
		{
			var playlists = spotifyConfigurationContainer.Spotify.Paginate(await spotifyConfigurationContainer.Spotify.Playlists.CurrentUsers(new PlaylistCurrentUsersRequest { Limit = 50 }).WaitAsync(cancellationToken).WithoutContextCapture(), cancellationToken: cancellationToken);
			var name = await nameProvider.WithoutContextCapture();
			var existingPlaylist = await playlists.FirstOrDefaultAsync(playlist => string.Equals(playlist.Name, name, StringComparison.OrdinalIgnoreCase), cancellationToken: cancellationToken).WithoutContextCapture();
			if (existingPlaylist != default)
				return await spotifyConfigurationContainer.GetPlaylist(existingPlaylist.Id, cancellationToken: cancellationToken).WithoutContextCapture();
			else
			{
				var userProfile = await spotifyConfigurationContainer.GetCurrentUserProfile(cancellationToken: cancellationToken).WithoutContextCapture();
				var userId = userProfile.Id;
				return await spotifyConfigurationContainer.Spotify.Playlists.Create(userId, new PlaylistCreateRequest(name) { Public = false, Collaborative = false }).WaitAsync(cancellationToken).WithoutContextCapture();
			}
		}
	}
}
