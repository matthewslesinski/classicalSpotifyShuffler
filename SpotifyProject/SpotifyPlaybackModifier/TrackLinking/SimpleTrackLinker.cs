using System;
using System.Collections.Generic;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using SpotifyProject.SpotifyPlaybackModifier.Transformations;
using SpotifyProject.Utils;
using System.Linq;

namespace SpotifyProject.SpotifyPlaybackModifier.TrackLinking
{
	public abstract class SimpleTrackLinker<ContextT, TrackT> : ITrackLinker<ContextT, TrackT, (string workName, string albumName)>
		where ContextT : ISpotifyPlaybackContext<TrackT>

	{
		public IEnumerable<ITrackGrouping<(string workName, string albumName), TrackT>> GroupTracksIntoWorks(ContextT originalContext, IEnumerable<TrackT> tracks)
		{
			var trackOrderWithinWorks = GetTrackOrderWithinWorks(originalContext);
			var groupings = tracks.GroupBy(track =>
			{
				var trackInfo = originalContext.GetMetadataForTrack(track);
				return (GetWorkNameForTrack(trackInfo), trackInfo.AlbumName);
			}).Select(group => new SimpleWork<TrackT>(group.Key.Item1, group.Key.AlbumName, group.OrderBy(trackOrderWithinWorks)));
			return groupings.ToList();
		}

		protected virtual IComparer<TrackT> GetTrackOrderWithinWorks(ContextT originalContext)
		{
			return ComparerUtils.ComparingBy<TrackT, (int discNumber, int trackNumber)>(t => originalContext.GetMetadataForTrack(t).AlbumIndex,
				ComparerUtils.ComparingBy<(int discNumber, int trackNumber)>(i => i.discNumber).ThenBy(i => i.trackNumber));
		}

		protected abstract string GetWorkNameForTrack(ITrackLinkingInfo trackInfo);


	}
}
