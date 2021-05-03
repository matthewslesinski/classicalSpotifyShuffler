using System;
using System.Collections.Generic;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts
{
	public interface ISpotifyQueue<TrackT> : ISpotifyAccessor
	{
		public IEnumerable<TrackT> PlaybackOrder { get; set; }
	}
}
