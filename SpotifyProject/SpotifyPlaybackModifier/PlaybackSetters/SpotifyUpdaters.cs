using System;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackSetters
{
	public class SpotifyUpdaters<TrackT>
	{
		public SpotifyUpdaters(SpotifyConfiguration config)
		{
			QueuePlaybackSetter = new QueuePlaybackSetter<TrackT>(config);
			PlaylistPlaybackSetter = new PlaylistPlaybackSetter<TrackT>(config, QueuePlaybackSetter, new BasicPlaylistTrackModifier(config));
			EfficientPlaylistPlaybackSetter = new PlaylistPlaybackSetter<TrackT>(config, QueuePlaybackSetter);
			EfficientPlaylistSetterWithoutPlayback = new PlaylistPlaybackSetter<TrackT>(config, null);
		}

		public IPlaybackSetter<ISpotifyPlaybackContext<TrackT>, PlaybackStateArgs> QueuePlaybackSetter { get; }
		public IPlaybackSetter<ISpotifyPlaybackContext<TrackT>, PlaybackStateArgs> PlaylistPlaybackSetter { get; }
		public IPlaybackSetter<ISpotifyPlaybackContext<TrackT>, PlaybackStateArgs> EfficientPlaylistPlaybackSetter { get; }
		public IPlaybackSetter<ISpotifyPlaybackContext<TrackT>, PlaybackStateArgs> EfficientPlaylistSetterWithoutPlayback { get; }
	}

}
