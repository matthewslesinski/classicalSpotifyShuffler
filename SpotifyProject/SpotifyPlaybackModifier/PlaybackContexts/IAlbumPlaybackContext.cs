using System;
using SpotifyAPI.Web;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts
{
	public interface IAlbumPlaybackContext : IStaticPlaybackContext<FullAlbum, SimpleTrack>
	{
		PlaybackContextType ISpotifyPlaybackContext<SimpleTrack>.ContextType => PlaybackContextType.Album;
		bool ISpotifyPlaybackContext<SimpleTrack>.TryGetSpotifyId(out string contextId)
		{
			contextId = SpotifyContext.Id;
			return true;
		}

		ITrackLinkingInfo<SimpleTrack> ISpotifyPlaybackContext<SimpleTrack>.GetMetadataForTrack(SimpleTrack track) => new SimpleTrackAndAlbumWrapper(track, SpotifyContext);
	}

	public interface IOriginalAlbumPlaybackContext : IAlbumPlaybackContext, ISimplePlaybackContext<FullAlbum, SimpleTrack>
	{
		IPaginatable<SimpleTrack> ISimplePlaybackContext<FullAlbum, SimpleTrack>.GetTracksFromSpotifyContext(FullAlbum spotifyContext) => spotifyContext.Tracks;
	}
}
