using System;
namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackSetters
{
	public class PlaybackStateArgs : IPlaybackSetterArgs
	{
		public string UriToPlay { get; set; }
		public int PositionToPlayMs { get; set; }
	}
}
