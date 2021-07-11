using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifyProject.Setup;
using System.Linq;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts
{

	public static class PlaybackContexts
	{
		private static readonly Func<SpotifyConfiguration, string, Task<IOriginalAlbumPlaybackContext>> SimpleAlbumContextConstructor =
			async (config, albumId) => await ExistingAlbumPlaybackContext.FromSimpleAlbum(config, albumId);
		private static readonly Func<SpotifyConfiguration, string, Task<IOriginalPlaylistPlaybackContext>> SimplePlaylistContextConstructor =
			async (config, playlistId) => await ExistingPlaylistPlaybackContext.FromSimplePlaylist(config, playlistId);
		private static readonly Func<SpotifyConfiguration, string, Task<IOriginalArtistPlaybackContext>> SimpleArtistContextConstructor =
			async (config, artistId) => await ExistingArtistPlaybackContext.FromSimpleArtist(config, artistId,
				Settings.Get<IEnumerable<string>>(SettingsName.ArtistAlbumIncludeGroups)
					.Select(value => Enum.Parse<ArtistsAlbumsRequest.IncludeGroups>(value, true)).Aggregate((ArtistsAlbumsRequest.IncludeGroups)0, (group1, group2) => group1 | group2));
		private static readonly Func<SpotifyConfiguration, string, Task<IOriginalAllLikedTracksPlaybackContext>> SimpleAllLikedSongsContextConstructor =
			(config, playlistId) => Task.FromResult<IOriginalAllLikedTracksPlaybackContext>(new ExistingAllLikedTracksPlaybackContext(config));


		private static readonly Dictionary<PlaybackContextType, object> SimplePlaybackContextConstructors =
			new Dictionary<PlaybackContextType, object>
		{
			{ PlaybackContextType.Album, SimpleAlbumContextConstructor },
			{ PlaybackContextType.Playlist,  SimplePlaylistContextConstructor},
			{ PlaybackContextType.Artist, SimpleArtistContextConstructor },
			{ PlaybackContextType.AllLikedTracks, SimpleAllLikedSongsContextConstructor }
		};

		public static bool TryGetExistingContextConstructorForType<ContextT, TrackT>(PlaybackContextType type, out Func<SpotifyConfiguration, string, Task<ContextT>> constructor)
			where ContextT : IOriginalPlaybackContext
		{
			constructor = null;
			return SimplePlaybackContextConstructors.TryGetValue(type, out var constructorObj)
				&& (constructor = constructorObj as Func<SpotifyConfiguration, string, Task<ContextT>>) != default;
		}
	}
}
