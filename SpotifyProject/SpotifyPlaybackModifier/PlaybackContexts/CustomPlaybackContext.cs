using System;
using System.Collections.Generic;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts
{
	public class CustomPlaybackContext : SpotifyPlaybackQueueBase<IPlayableTrackLinkingInfo>, ICustomPlaybackContext
	{
		public CustomPlaybackContext(SpotifyConfiguration spotifyConfiguration, IEnumerable<IPlayableTrackLinkingInfo> tracks) : base(spotifyConfiguration)
		{
			PlaybackOrder = tracks;
		}
	}

	public class ReorderedCustomPlaybackContext<OriginalContextT> : CustomPlaybackContext, IReorderedPlaybackContext<IPlayableTrackLinkingInfo, OriginalContextT> where OriginalContextT : ICustomPlaybackContext
	{
		public ReorderedCustomPlaybackContext(OriginalContextT originalContext, IEnumerable<IPlayableTrackLinkingInfo> tracks) : base(originalContext.SpotifyConfiguration, tracks)
		{
			BaseContext = originalContext;
		}

		public OriginalContextT BaseContext { get; }

		public static ReorderedCustomPlaybackContext<OriginalContextT> FromContextAndTracks(OriginalContextT originalContext, IEnumerable<IPlayableTrackLinkingInfo> tracks) =>
			new ReorderedCustomPlaybackContext<OriginalContextT>(originalContext, tracks);
	}
}

