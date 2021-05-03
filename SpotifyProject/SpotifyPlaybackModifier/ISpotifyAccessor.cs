using System;
using SpotifyAPI.Web;

namespace SpotifyProject.SpotifyPlaybackModifier
{
	public interface ISpotifyAccessor
	{
		SpotifyConfiguration SpotifyConfiguration { get; }
		SpotifyClient Spotify => SpotifyConfiguration.Spotify;
	}
}
