using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpotifyAPI.Web;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts
{
	public interface ISimplePlaybackContext<SpotifyItemT, TrackT> : IStaticPlaybackContext<SpotifyItemT, TrackT>, IOriginalPlaybackContext<TrackT>
	{
		IPaginatable<TrackT> GetTracksFromSpotifyContext(SpotifyItemT spotifyContext);

		async Task IOriginalPlaybackContext<TrackT>.FullyLoad()
		{
			var allTracks = await Spotify.PaginateAll(GetTracksFromSpotifyContext(SpotifyContext));
			PlaybackOrder = allTracks;
		}
	}
}
