using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using ApplicationResources.Setup;
using System.Linq;
using CustomResources.Utils.Extensions;

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
				Settings.Get<IEnumerable<string>>(BasicSettings.ArtistAlbumIncludeGroups)
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

        public static bool TryGetExistingContextConstructorForType<ContextT, TrackT>(PlaybackContextType type, out ContextConstructor<ContextT> constructor)
            where ContextT : IOriginalPlaybackContext => SimplePlaybackContextConstructors.TryGetCastedValue(type, out constructor);
    }
}
