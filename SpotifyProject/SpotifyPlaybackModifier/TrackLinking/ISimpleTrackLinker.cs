using System;
using System.Collections.Generic;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using SpotifyProject.SpotifyPlaybackModifier.Transformations;
using SpotifyProject.Utils;
using System.Linq;

namespace SpotifyProject.SpotifyPlaybackModifier.TrackLinking
{
	public interface IMetadataBasedTrackLinker<ContextT, TrackT, WorkT> : ITrackLinker<ContextT, TrackT, WorkT>
		where ContextT : ISpotifyPlaybackContext<TrackT>
	{
		IEnumerable<ITrackGrouping<WorkT, TrackT>> ITrackLinker<ContextT, TrackT, WorkT>.GroupTracksIntoWorks(ContextT originalContext, IEnumerable<TrackT> tracks)
		{
			var trackOrderWithinWorks = GetTrackOrdererWithinWorks(originalContext);
			var metadata = tracks.Select(originalContext.GetMetadataForTrack);
			var groupings = GroupTracksIntoWorks(metadata);
			var works = groupings.Select(group => DesignateTracksToWork(group.Key, group.OrderBy(trackOrderWithinWorks)));
			return works.ToList();
		}

		protected IComparer<ITrackLinkingInfo<TrackT>> GetTrackOrdererWithinWorks(ContextT originalContext);

		protected IEnumerable<IGrouping<WorkT, ITrackLinkingInfo<TrackT>>> GroupTracksIntoWorks(IEnumerable<ITrackLinkingInfo<TrackT>> trackMetadata);

		protected ITrackGrouping<WorkT, TrackT> DesignateTracksToWork(WorkT work, IEnumerable<ITrackLinkingInfo<TrackT>> tracksInWork);	
	}

	public interface ISimpleTrackLinker<ContextT, TrackT, WorkT> : IMetadataBasedTrackLinker<ContextT, TrackT, WorkT>
		where ContextT : ISpotifyPlaybackContext<TrackT>
	{
		IComparer<ITrackLinkingInfo<TrackT>> IMetadataBasedTrackLinker<ContextT, TrackT, WorkT>.GetTrackOrdererWithinWorks(ContextT originalContext) =>
			ITrackLinkingInfo.TrackOrderWithinAlbums;
	}

	public interface ISimpleTrackLinkerByWorkName<ContextT, TrackT> : ISimpleTrackLinker<ContextT, TrackT, (string workName, string albumName, string albumUri)>
		where ContextT : ISpotifyPlaybackContext<TrackT>

	{
		IEnumerable<IGrouping<(string workName, string albumName, string albumUri), ITrackLinkingInfo<TrackT>>> IMetadataBasedTrackLinker<ContextT, TrackT, (string workName, string albumName, string albumUri)>
			.GroupTracksIntoWorks(IEnumerable<ITrackLinkingInfo<TrackT>> trackMetadata) =>
				trackMetadata.GroupBy(track => (GetWorkNameForTrack(track), track.AlbumName, track.AlbumUri));

		ITrackGrouping<(string workName, string albumName, string albumUri), TrackT> IMetadataBasedTrackLinker<ContextT, TrackT, (string workName, string albumName, string albumUri)>
			.DesignateTracksToWork((string workName, string albumName, string albumUri) work, IEnumerable<ITrackLinkingInfo<TrackT>> tracksInWork) =>
				new SimpleWork<TrackT>(work.workName, work.albumName, work.albumUri, tracksInWork);

		protected string GetWorkNameForTrack(ITrackLinkingInfo trackInfo);
	}
}
