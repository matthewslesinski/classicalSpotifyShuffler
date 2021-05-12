using System;
using System.Linq;
using System.Collections.Generic;
using SpotifyAPI.Web;
using System.Threading.Tasks;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts
{
	public abstract class PlaylistPlaybackContext : SpotifyPlaybackQueueBase<FullTrack>, IPlaylistPlaybackContext
	{
		public PlaylistPlaybackContext(SpotifyConfiguration spotifyConfiguration, FullPlaylist playlist) : base(spotifyConfiguration)
		{
			SpotifyContext = playlist;
		}

		public ITrackLinkingInfo<FullTrack> GetMetadataForTrack(FullTrack track)
		{
			return new FullTrackWrapper(track);
		}

		public FullPlaylist SpotifyContext { get; }
	}

	public class ExistingPlaylistPlaybackContext : PlaylistPlaybackContext, IOriginalPlaylistPlaybackContext
	{
		public ExistingPlaylistPlaybackContext(SpotifyConfiguration spotifyConfiguration, FullPlaylist playlist) : base(spotifyConfiguration, playlist)
		{
		}

		public static async Task<ExistingPlaylistPlaybackContext> FromSimplePlaylist(SpotifyConfiguration spotifyConfiguration, string playlistId)
		{
			var fullPlaylist = await spotifyConfiguration.Spotify.Playlists.Get(playlistId);
			return new ExistingPlaylistPlaybackContext(spotifyConfiguration, fullPlaylist);
		}

		public async Task FullyLoad()
		{
			Logger.Information($"Loading tracks for playlist with Id {SpotifyContext.Id}");
			var allItems = Spotify.Paginate(await Spotify.Playlists.GetItems(SpotifyContext.Id, new PlaylistGetItemsRequest { Limit = 100, Market = _relevantMarket }));
			var allTracks = await allItems.Select(track => track.Track).OfType<FullTrack>().ToListAsync();
			Logger.Information($"Loaded {allTracks.Count()} tracks");
			PlaybackOrder = allTracks;
		}
	}

	public class ReorderedPlaylistPlaybackContext<OriginalContextT> : PlaylistPlaybackContext, IReorderedPlaybackContext<FullTrack, OriginalContextT>
		where OriginalContextT : IPlaylistPlaybackContext
	{
		public ReorderedPlaylistPlaybackContext(OriginalContextT baseContext, IEnumerable<FullTrack> reorderedTracks) : base(baseContext.SpotifyConfiguration, baseContext.SpotifyContext)
		{
			PlaybackOrder = reorderedTracks;
			BaseContext = baseContext;
		}

		public OriginalContextT BaseContext { get; }

		public static ReorderedPlaylistPlaybackContext<OriginalContextT> FromContextAndTracks(OriginalContextT originalContext, IEnumerable<FullTrack> tracks) =>
			new ReorderedPlaylistPlaybackContext<OriginalContextT>(originalContext, tracks);
	}
}
