using System;
using System.Collections.Generic;
using System.Collections;
using System.Collections.Immutable;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackSetters
{
	public interface IPlaybackSetterArgs : IContextSetterArgs
	{
		bool? AllowUsingContextUri { get; set; }
	}

	public interface IPlaybackStateArgs : IPlaybackSetterArgs
	{
		string UriToPlay { get; set; }
		int? PositionToPlayMs { get; set; }
		bool? CurrentPlaybackFound { get; set; }
	}

	public class PlaybackStateArgs : IPlaybackStateArgs
	{
		public string UriToPlay { get; set; } = null;
		public int? PositionToPlayMs { get; set; } = null;
		public bool? AllowUsingContextUri { get; set; } = false;
		public bool? CurrentPlaybackFound { get; set; } = false;
	}
}
