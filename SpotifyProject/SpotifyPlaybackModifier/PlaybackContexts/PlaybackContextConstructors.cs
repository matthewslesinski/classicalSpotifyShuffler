using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using System.Linq;
using CustomResources.Utils.Extensions;
using SpotifyProject.Configuration;
using ApplicationResources.ApplicationUtils.Parameters;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts
{
	public static class PlaybackContextConstructors
	{
		public delegate Task<ContextT> ContextConstructor<ContextT>(SpotifyConfiguration config, string contextId) where ContextT : IOriginalPlaybackContext;

		private static readonly ContextConstructor<IOriginalAlbumPlaybackContext> SimpleAlbumContextConstructor =
			async (config, albumId) => await ExistingAlbumPlaybackContext.FromSimpleAlbum(config, albumId).WithoutContextCapture();
		private static readonly ContextConstructor<IOriginalPlaylistPlaybackContext> SimplePlaylistContextConstructor =
			async (config, playlistId) => await ExistingPlaylistPlaybackContext.FromSimplePlaylist(config, playlistId).WithoutContextCapture();
		private static readonly ContextConstructor<IOriginalArtistPlaybackContext> SimpleArtistContextConstructor =
			async (config, artistId) => await ExistingArtistPlaybackContext.FromSimpleArtist(config, artistId,
				TaskParameters.Get<IEnumerable<string>>(SpotifyParameters.ArtistAlbumIncludeGroups)
					.Select(value => Enum.Parse<ArtistsAlbumsRequest.IncludeGroups>(value, true))
					.Aggregate((ArtistsAlbumsRequest.IncludeGroups)0, (group1, group2) => group1 | group2)).WithoutContextCapture();
		private static readonly ContextConstructor<IOriginalAllLikedTracksPlaybackContext> SimpleAllLikedSongsContextConstructor =
			(config, playlistId) => Task.FromResult<IOriginalAllLikedTracksPlaybackContext>(new ExistingAllLikedTracksPlaybackContext(config));


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
			static async Task<ContextT> GetLoadedContext(SpotifyConfiguration config, string contextId, ContextConstructor<ContextT> foundConstructor)
			{
				var context = await foundConstructor(config, contextId).WithoutContextCapture();
				await context.FullyLoad().WithoutContextCapture();
				return context;
			}

			var didFindConstructor = SimplePlaybackContextConstructors.TryGetCastedValue<PlaybackContextType, ContextConstructor<ContextT>>(type, out var foundConstructor);
			constructor = didFindConstructor && loadContext ? (config, contextId) => GetLoadedContext(config, contextId, foundConstructor) : foundConstructor;
			return didFindConstructor;
		}
	}
}
