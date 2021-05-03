using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpotifyAPI.Web;

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
