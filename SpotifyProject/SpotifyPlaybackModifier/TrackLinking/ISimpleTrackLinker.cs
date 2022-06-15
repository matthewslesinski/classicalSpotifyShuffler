using System;
using System.Collections.Generic;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using SpotifyProject.SpotifyPlaybackModifier.Transformations;
using CustomResources.Utils.Extensions;
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

	public interface ISimpleTrackLinkerByWorkName<ContextT, TrackT> : ISimpleTrackLinker<ContextT, TrackT, WorkNameKey>
		where ContextT : ISpotifyPlaybackContext<TrackT>

	{
		IEnumerable<IGrouping<WorkNameKey, ITrackLinkingInfo<TrackT>>> IMetadataBasedTrackLinker<ContextT, TrackT, WorkNameKey>
			.GroupTracksIntoWorks(IEnumerable<ITrackLinkingInfo<TrackT>> trackMetadata) =>
				trackMetadata.GroupBy(track => new WorkNameKey(GetWorkNameForTrack(track), track.AlbumName, track.AlbumUri));

		ITrackGrouping<WorkNameKey, TrackT> IMetadataBasedTrackLinker<ContextT, TrackT, WorkNameKey>
			.DesignateTracksToWork(WorkNameKey work, IEnumerable<ITrackLinkingInfo<TrackT>> tracksInWork) =>
				new SimpleWork<TrackT>(work.WorkName, work.AlbumName, work.AlbumUri, tracksInWork);

		public string GetWorkNameForTrack(ITrackLinkingInfo trackInfo);
	}

	public record WorkNameKey(string WorkName, string AlbumName, string AlbumUri);
}
