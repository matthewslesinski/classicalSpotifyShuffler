using System;
using SpotifyAPI.Web;

namespace SpotifyProject.SpotifyPlaybackModifier
{
	public class SpotifyConfiguration : ISpotifyConfigurationContainer
	{
		public SpotifyClient Spotify { get; set; }
		public string Market { get; set; }

		SpotifyConfiguration ISpotifyConfigurationContainer.SpotifyConfiguration => this;
	}
}
