
namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts
{
	public interface IStaticPlaybackContext<SpotifyItemT, TrackT> : ISpotifyPlaybackContext<TrackT>
	{
		SpotifyItemT SpotifyContext { get; }
	}
}
