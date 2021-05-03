using System;
using SpotifyAPI.Web;

namespace SpotifyProject.SpotifyPlaybackModifier
{
	public class SpotifyAccessorBase : ISpotifyAccessor
	{
		public SpotifyAccessorBase(SpotifyConfiguration spotifyConfiguration)
		{
			SpotifyConfiguration = spotifyConfiguration;
		}

		public SpotifyConfiguration SpotifyConfiguration { get; }
		public SpotifyClient Spotify => SpotifyConfiguration.Spotify;

	}
}
