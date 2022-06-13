using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using System.Linq;
using CustomResources.Utils.Extensions;
using SpotifyProject.Configuration;
using ApplicationResources.ApplicationUtils.Parameters;
using System.Threading;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts
{
	public static class PlaybackContextConstructors
	{
		public delegate Task<ContextT> ContextConstructor<ContextT>(SpotifyConfiguration config, string contextId, CancellationToken cancellationToken = default) where ContextT : IOriginalPlaybackContext;

		private static readonly ContextConstructor<IOriginalAlbumPlaybackContext> SimpleAlbumContextConstructor =
			async (config, albumId, cancellationToken) => await ExistingAlbumPlaybackContext.FromSimpleAlbum(config, albumId, cancellationToken).WithoutContextCapture();
		private static readonly ContextConstructor<IOriginalPlaylistPlaybackContext> SimplePlaylistContextConstructor =
			async (config, playlistId, cancellationToken) => await ExistingPlaylistPlaybackContext.FromSimplePlaylist(config, playlistId, cancellationToken).WithoutContextCapture();
		private static readonly ContextConstructor<IOriginalArtistPlaybackContext> SimpleArtistContextConstructor =
			async (config, artistId, cancellationToken) => await ExistingArtistPlaybackContext.FromSimpleArtist(config, artistId,
				TaskParameters.Get<ArtistsAlbumsRequest.IncludeGroups>(SpotifyParameters.ArtistAlbumIncludeGroups), cancellationToken).WithoutContextCapture();
		private static readonly ContextConstructor<IOriginalAllLikedTracksPlaybackContext> SimpleAllLikedSongsContextConstructor =
			(config, playlistId, cancellationToken) => Task.FromResult<IOriginalAllLikedTracksPlaybackContext>(new ExistingAllLikedTracksPlaybackContext(config));


		private static readonly Dictionary<PlaybackContextType, object> SimplePlaybackContextConstructors =
			new Dictionary<PlaybackContextType, object>
		{
			{ PlaybackContextType.Album, SimpleAlbumContextConstructor },
			{ PlaybackContextType.Playlist,  SimplePlaylistContextConstructor},
			{ PlaybackContextType.Artist, SimpleArtistContextConstructor },
			{ PlaybackContextType.AllLikedTracks, SimpleAllLikedSongsContextConstructor }
		};

		public static bool TryGetExistingContextConstructorForType<ContextT, TrackT>(PlaybackContextType type, out ContextConstructor<ContextT> constructor, bool loadContext = true)
				where ContextT : IOriginalPlaybackContext
		{
			static async Task<ContextT> GetLoadedContext(SpotifyConfiguration config, string contextId, ContextConstructor<ContextT> foundConstructor, CancellationToken cancellationToken)
			{
				var context = await foundConstructor(config, contextId, cancellationToken).WithoutContextCapture();
				await context.FullyLoad(cancellationToken).WithoutContextCapture();
				return context;
			}

			var didFindConstructor = SimplePlaybackContextConstructors.TryGetCastedValue<PlaybackContextType, ContextConstructor<ContextT>>(type, out var foundConstructor);
			constructor = didFindConstructor && loadContext ? (config, contextId, cancellationToken) => GetLoadedContext(config, contextId, foundConstructor, cancellationToken) : foundConstructor;
			return didFindConstructor;
		}
	}
}
