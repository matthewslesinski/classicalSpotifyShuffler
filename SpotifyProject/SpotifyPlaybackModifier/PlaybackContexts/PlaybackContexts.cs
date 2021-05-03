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
		private static readonly Func<SpotifyConfiguration, string, Task<IOriginalPlaybackContext<FullAlbum, SimpleTrack>>> SimpleAlbumContextConstructor =
			async (config, albumId) => await ExistingAlbumPlaybackContext.FromSimpleAlbum(config, albumId);
		private static readonly Func<SpotifyConfiguration, string, Task<IOriginalPlaybackContext<FullPlaylist, FullTrack>>> SimplePlaylistContextConstructor =
			async (config, playlistId) => await ExistingPlaylistPlaybackContext.FromSimplePlaylist(config, playlistId);
		private static readonly Func<SpotifyConfiguration, string, Task<IOriginalPlaybackContext<FullArtist, SimpleTrackAndAlbumWrapper>>> SimpleArtistContextConstructor =
			async (config, artistId) => await ExistingArtistPlaybackContext.FromSimpleArtist(config, artistId,
				GlobalCommandLine.Store.GetOptionValue<IEnumerable<string>>(CommandLineOptions.Names.ArtistAlbumIncludeGroups)
					.Select(value => Enum.Parse<ArtistsAlbumsRequest.IncludeGroups>(value, true)).Aggregate((ArtistsAlbumsRequest.IncludeGroups)0, (group1, group2) => group1 | group2));

		private static readonly Dictionary<PlaybackContextType, object> SimplePlaybackContextConstructors =
			new Dictionary<PlaybackContextType, object>
		{
			{ PlaybackContextType.Album, SimpleAlbumContextConstructor },
			{ PlaybackContextType.Playlist,  SimplePlaylistContextConstructor},
			{ PlaybackContextType.Artist, SimpleArtistContextConstructor }
		};

		public static bool TryGetExistingContextConstructorForType<SpotifyItemT, TrackT>(PlaybackContextType type, out Func<SpotifyConfiguration, string, Task<IOriginalPlaybackContext<SpotifyItemT, TrackT>>> constructor)
		{
			constructor = null;
			return SimplePlaybackContextConstructors.TryGetValue(type, out var constructorObj)
				&& (constructor = constructorObj as Func<SpotifyConfiguration, string, Task<IOriginalPlaybackContext<SpotifyItemT, TrackT>>>) != default;
		}
	}
}
