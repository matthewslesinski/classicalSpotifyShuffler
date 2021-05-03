using System;
using System.Threading.Tasks;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts
{
	public interface IStaticPlaybackContext<SpotifyItemT, TrackT> : ISpotifyPlaybackContext<TrackT>
	{
		SpotifyItemT SpotifyContext { get; }
	}

	public interface IOriginalPlaybackContext<SpotifyItemT, TrackT> : IStaticPlaybackContext<SpotifyItemT, TrackT>
	{
		Task FullyLoad();
	}
}
