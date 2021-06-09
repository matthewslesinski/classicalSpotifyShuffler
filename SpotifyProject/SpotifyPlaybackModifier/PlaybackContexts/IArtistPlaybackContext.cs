using System;
using SpotifyAPI.Web;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;
using static SpotifyProject.Utils.SpotifyConstants;

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
		bool ISpotifyPlaybackContext.TryGetSpotifyUri(out string contextUri)
		{
			contextUri = SpotifyContext.Uri;
			return true;
		}

		SpotifyElementType IStaticPlaybackContext<FullArtist, SimpleTrackAndAlbumWrapper>.SpotifyElementType => SpotifyElementType.Artist;

	}

	public interface IOriginalArtistPlaybackContext : IArtistPlaybackContext, IOriginalPlaybackContext
	{
	}
}
