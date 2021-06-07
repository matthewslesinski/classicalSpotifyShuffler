using System;
using SpotifyAPI.Web;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts
{
	public interface IArtistPlaybackContext : IStaticPlaybackContext<FullArtist, SimpleTrackAndAlbumWrapper>, IProcessedTrackPlaybackContext<SimpleTrack, SimpleTrackAndAlbumWrapper>
	{
		PlaybackContextType ISpotifyPlaybackContext.ContextType => PlaybackContextType.Artist;
		bool ISpotifyPlaybackContext.TryGetSpotifyId(out string contextId)
		{
			contextId = SpotifyContext.Id;
			return true;
		}
	}

	public interface IOriginalArtistPlaybackContext : IArtistPlaybackContext, IOriginalPlaybackContext
	{
	}
}
