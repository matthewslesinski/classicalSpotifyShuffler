using System;
using System.Collections.Generic;
using System.Collections;
using System.Collections.Immutable;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackSetters
{
	public class PlaybackStateArgs : IPlaybackSetterArgs
	{
		public string UriToPlay { get; set; } = null;
		public int? PositionToPlayMs { get; set; } = null;
		public bool? AllowUsingContextUri { get; set; } = false;
		public bool? CurrentPlaybackFound {get; set; } = false;
	}
}
