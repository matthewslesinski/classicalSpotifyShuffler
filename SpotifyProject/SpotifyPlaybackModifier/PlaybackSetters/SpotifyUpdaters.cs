using System;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackSetters
{
	public class SpotifyUpdaters<TrackT>
	{
		public SpotifyUpdaters(SpotifyConfiguration config)
		{
			QueuePlaybackSetter = new QueuePlaybackSetter<TrackT>(config);
		}

		public IPlaybackSetter<ISpotifyPlaybackContext<TrackT>, PlaybackStateArgs> QueuePlaybackSetter { get; }
	}
 
}
