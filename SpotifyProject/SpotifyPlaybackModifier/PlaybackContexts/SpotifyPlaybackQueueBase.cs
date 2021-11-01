using System;
using System.Collections.Generic;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts
{
	public abstract class SpotifyPlaybackQueueBase<TrackT> : SpotifyAccessorBase, ISpotifyQueue<TrackT>
	{
		protected readonly string _relevantMarket;
		public SpotifyPlaybackQueueBase(SpotifyConfiguration spotifyConfiguration) : base(spotifyConfiguration)
		{
			_relevantMarket = spotifyConfiguration.Market;
		}

		public IEnumerable<TrackT> PlaybackOrder { get; set; }
	}
}
