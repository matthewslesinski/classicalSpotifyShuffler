using System;
namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts
{
	public interface IReorderedPlaybackContext<TrackT, out BaseContextT> : ISpotifyPlaybackContext<TrackT> where BaseContextT : ISpotifyPlaybackContext<TrackT>
	{
		BaseContextT BaseContext { get; }
	}
}
