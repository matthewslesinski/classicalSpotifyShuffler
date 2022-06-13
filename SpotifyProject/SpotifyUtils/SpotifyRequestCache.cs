using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApplicationResources.Logging;
using CustomResources.Utils.Concepts.DataStructures;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;
using SpotifyAPI.Web;
using SpotifyProject.SpotifyPlaybackModifier;

namespace SpotifyProject.SpotifyUtils
{
	public abstract class SpotifyRequestCache<T, ResponseT, RequestDescriptionT> : IndexedQueryCache<T, ResponseT>, IGlobalSpotifyServiceUser where ResponseT : class, IFinitePaginatable, IPaginatable<T>
	{
		private readonly SpotifyConfiguration _spotifyConfig;

		public SpotifyRequestCache(int maxBatchSize, SpotifyConfiguration spotify = null, LoadType loadType = LoadType.Lazy, ResponseT knownInitialValues = null)
			: base(maxBatchSize, loadType, knownInitialValues?.Total, knownInitialValues?.Items)
		{
			_spotifyConfig = spotify;
		}

		protected SpotifyClient Spotify => _spotifyConfig?.Spotify ?? this.AccessSpotify().Client;
		protected string Market => _spotifyConfig?.Market ?? this.AccessSpotify().SpotifyConfiguration.Market;

		protected override int GetTotalFromResponse(ResponseT response) =>
			response.Total ?? Exceptions.Throw<int>(new ArgumentException("The first page must indicate the total number of items and the starting offset in order to paginate concurrently.", nameof(response.Total)));

		protected override IList<T> GetValuesFromResponse(ResponseT response) =>
			response.Items ?? Exceptions.Throw<IList<T>>(new ArgumentException("The page must contain some items", nameof(response.Items)));

		protected override Task<ResponseT> GetPage(int startIndex, int count, CancellationToken cancellationToken = default) =>
			DoRequest(GenerateRequest(startIndex, count)).WaitAsync(cancellationToken);

		protected abstract RequestDescriptionT GenerateRequest(int startIndex, int count);
		protected abstract Task<ResponseT> DoRequest(RequestDescriptionT requestSpecification);

	}

	public class SavedAlbumsCache : SpotifyRequestCache<SavedAlbum, Paging<SavedAlbum>, LibraryAlbumsRequest>
	{
		public SavedAlbumsCache(SpotifyConfiguration spotify = null, LoadType loadType = LoadType.Lazy, Paging<SavedAlbum> firstPage = null)
			: base(50, spotify, loadType, firstPage)
		{
		}

		protected override async Task<Paging<SavedAlbum>> DoRequest(LibraryAlbumsRequest requestSpecification)
		{
			var results = await Spotify.Library.GetAlbums(requestSpecification).WithoutContextCapture();
			return results;
		}

		protected override LibraryAlbumsRequest GenerateRequest(int startIndex, int count) =>
			new LibraryAlbumsRequest { Limit = count, Market = Market, Offset = startIndex };
	}

	public class SavedPlaylistsCache : SpotifyRequestCache<SimplePlaylist, Paging<SimplePlaylist>, PlaylistCurrentUsersRequest>
	{
		public SavedPlaylistsCache(SpotifyConfiguration spotify = null, LoadType loadType = LoadType.Lazy, Paging<SimplePlaylist> firstPage = null)
			: base(50, spotify, loadType, firstPage)
		{
		}

		protected override Task<Paging<SimplePlaylist>> DoRequest(PlaylistCurrentUsersRequest requestSpecification) =>
			Spotify.Playlists.CurrentUsers(requestSpecification);

		protected override PlaylistCurrentUsersRequest GenerateRequest(int startIndex, int count) =>
			new PlaylistCurrentUsersRequest { Limit = count, Offset = startIndex };
	}

	public class AlbumTracksCache : SpotifyRequestCache<SimpleTrack, Paging<SimpleTrack>, AlbumTracksRequest>
	{
		private readonly string _albumId;
		public AlbumTracksCache(string albumId, SpotifyConfiguration spotify = null, LoadType loadType = LoadType.Lazy, Paging<SimpleTrack> firstPage = null)
			: base(50, spotify, loadType, firstPage)
		{
			_albumId = albumId;
		}

		protected override Task<Paging<SimpleTrack>> DoRequest(AlbumTracksRequest requestSpecification) =>
			Spotify.Albums.GetTracks(_albumId, requestSpecification);

		protected override AlbumTracksRequest GenerateRequest(int startIndex, int count) =>
			new AlbumTracksRequest { Limit = count, Offset = startIndex, Market = Market };
	}

	public class PlaylistTracksCache : SpotifyRequestCache<PlaylistTrack<IPlayableItem>, Paging<PlaylistTrack<IPlayableItem>>, PlaylistGetItemsRequest>, IQueryCache<FullTrack>
	{
		private readonly string _playlistId;
		public PlaylistTracksCache(string playlistid, SpotifyConfiguration spotify = null, LoadType loadType = LoadType.Lazy, Paging<PlaylistTrack<IPlayableItem>> firstPage = null)
			: base(50, spotify, loadType, firstPage)
		{
			_playlistId = playlistid;
		}

		protected override Task<Paging<PlaylistTrack<IPlayableItem>>> DoRequest(PlaylistGetItemsRequest requestSpecification) =>
			Spotify.Playlists.GetItems(_playlistId, requestSpecification);

		protected override PlaylistGetItemsRequest GenerateRequest(int startIndex, int count) =>
			new PlaylistGetItemsRequest { Limit = count, Offset = startIndex, Market = Market };

		async Task<List<FullTrack>> IQueryCache<FullTrack>.GetAll(CancellationToken cancellationToken) =>
			(await GetAll(cancellationToken).WithoutContextCapture()).Select(playlistTrack => playlistTrack.Track).OfType<FullTrack>().ToList();

		async Task<List<FullTrack>> IQueryCache<FullTrack>.GetSubsequence(int start, int count, CancellationToken cancellationToken) =>
			(await GetSubsequence(start, count, cancellationToken).WithoutContextCapture()).Select(playlistTrack => playlistTrack.Track).OfType<FullTrack>().ToList();
	}

	public class AllLikedTracksCache : SpotifyRequestCache<SavedTrack, Paging<SavedTrack>, LibraryTracksRequest>, IQueryCache<FullTrack>
	{
		public AllLikedTracksCache(SpotifyConfiguration spotify = null, LoadType loadType = LoadType.Lazy, Paging<SavedTrack> firstPage = null)
			: base(50, spotify, loadType, firstPage)
		{
		}

		protected override Task<Paging<SavedTrack>> DoRequest(LibraryTracksRequest requestSpecification) =>
			Spotify.Library.GetTracks(requestSpecification);

		protected override LibraryTracksRequest GenerateRequest(int startIndex, int count) =>
			new LibraryTracksRequest { Limit = count, Offset = startIndex, Market = Market };

		async Task<List<FullTrack>> IQueryCache<FullTrack>.GetAll(CancellationToken cancellationToken) =>
			(await GetAll(cancellationToken).WithoutContextCapture()).Select(savedTrack => savedTrack.Track).ToList();

		async Task<List<FullTrack>> IQueryCache<FullTrack>.GetSubsequence(int start, int count, CancellationToken cancellationToken) =>
			(await GetSubsequence(start, count, cancellationToken).WithoutContextCapture()).Select(savedTrack => savedTrack.Track).ToList();
	}
}

