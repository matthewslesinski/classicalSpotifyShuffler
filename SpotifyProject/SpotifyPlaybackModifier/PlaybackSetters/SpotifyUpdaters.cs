using System;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackSetters
{
	public class SpotifyUpdaters<TrackT>
	{
		public SpotifyUpdaters(SpotifyConfiguration config)
		{
			QueuePlaybackSetter = new QueuePlaybackSetter<TrackT>(config);
			PlaylistPlaybackSetter = new PlaylistSetter<TrackT>(config, QueuePlaybackSetter, new BasicPlaylistTrackModifier(config));
			EfficientPlaylistPlaybackSetter = new PlaylistSetter<TrackT>(config, QueuePlaybackSetter);
			EfficientPlaylistSetterWithoutPlayback = new PlaylistSetter<TrackT>(config, null);
		}

		public IContextSetter<ISpotifyPlaybackContext<TrackT>, IPlaybackSetterArgs> QueuePlaybackSetter { get; }
		public IContextSetter<ISpotifyPlaybackContext<TrackT>, IPlaybackSetterArgs> PlaylistPlaybackSetter { get; }
		public IContextSetter<ISpotifyPlaybackContext<TrackT>, IPlaybackSetterArgs> EfficientPlaylistPlaybackSetter { get; }
		public IContextSetter<ISpotifyPlaybackContext<TrackT>, IPlaybackSetterArgs> EfficientPlaylistSetterWithoutPlayback { get; }
	}

}
