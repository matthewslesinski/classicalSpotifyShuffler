using System;
using SpotifyAPI.Web;

namespace SpotifyProject.SpotifyPlaybackModifier
{
	public class SpotifyConfiguration
	{
		public SpotifyClient Spotify { get; set; }
		public string Market { get; set; }
	}
}
