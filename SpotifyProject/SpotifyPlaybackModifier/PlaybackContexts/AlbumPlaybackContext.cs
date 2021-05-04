using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts
{
	public abstract class AlbumPlaybackContext : SpotifyPlaybackQueueBase<SimpleTrack>, IAlbumPlaybackContext
	{
		public AlbumPlaybackContext(SpotifyConfiguration spotifyConfiguration, FullAlbum album) : base(spotifyConfiguration)
		{
			SpotifyContext = album;
		}

		public FullAlbum SpotifyContext { get; }
	}

	public class ExistingAlbumPlaybackContext : AlbumPlaybackContext, IOriginalAlbumPlaybackContext
	{
		public ExistingAlbumPlaybackContext(SpotifyConfiguration spotifyConfiguration, FullAlbum album) : base(spotifyConfiguration, album)
		{
		}

		public static async Task<ExistingAlbumPlaybackContext> FromSimpleAlbum(SpotifyConfiguration spotifyConfiguration, string albumId)
		{
			var fullAlbum = await spotifyConfiguration.Spotify.Albums.Get(albumId, new AlbumRequest { Market = spotifyConfiguration.Market });
			return new ExistingAlbumPlaybackContext(spotifyConfiguration, fullAlbum);
		}

	}

	public class ReorderedAlbumPlaybackContext<OriginalContextT> : AlbumPlaybackContext, IReorderedPlaybackContext<SimpleTrack, OriginalContextT>
		where OriginalContextT : IAlbumPlaybackContext
	{
		public ReorderedAlbumPlaybackContext(OriginalContextT baseContext, IEnumerable<SimpleTrack> reorderedTracks) : base(baseContext.SpotifyConfiguration, baseContext.SpotifyContext)
		{
			PlaybackOrder = reorderedTracks;
			BaseContext = baseContext;
		}

		public OriginalContextT BaseContext { get; }

		public static ReorderedAlbumPlaybackContext<OriginalContextT> FromContextAndTracks(OriginalContextT originalContext, IEnumerable<SimpleTrack> tracks) =>
			new ReorderedAlbumPlaybackContext<OriginalContextT>(originalContext, tracks);
	}
}
