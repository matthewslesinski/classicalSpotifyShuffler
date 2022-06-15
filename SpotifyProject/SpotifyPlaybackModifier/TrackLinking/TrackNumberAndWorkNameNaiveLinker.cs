using System;
using System.Collections.Generic;
using System.Linq;
using CustomResources.Utils.Extensions;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using SpotifyProject.SpotifyPlaybackModifier.Transformations;

namespace SpotifyProject.SpotifyPlaybackModifier.TrackLinking
{
	public class TrackNumberAndWorkNameNaiveLinker<ContextT, TrackT> : ISimpleTrackLinker<ContextT, TrackT, WorkNameKey>
		where ContextT : ISpotifyPlaybackContext<TrackT>
	{

		private readonly ISimpleTrackLinkerByWorkName<ContextT, TrackT> _workNameLinker;
		public TrackNumberAndWorkNameNaiveLinker(ISimpleTrackLinkerByWorkName<ContextT, TrackT> workNameLinker)
		{
			_workNameLinker = workNameLinker;
		}


		ITrackGrouping<WorkNameKey, TrackT> IMetadataBasedTrackLinker<ContextT, TrackT, WorkNameKey>
			.DesignateTracksToWork(WorkNameKey work, IEnumerable<ITrackLinkingInfo<TrackT>> tracksInWork) =>
				new SimpleWork<TrackT>(work.WorkName, work.AlbumName, work.AlbumUri, tracksInWork);

		public IEnumerable<IGrouping<WorkNameKey, ITrackLinkingInfo<TrackT>>> GroupTracksIntoWorks(IEnumerable<ITrackLinkingInfo<TrackT>> trackMetadata)
		{
			var groupedTracks = trackMetadata.GroupBy(trackMetadata => trackMetadata.AlbumUri)
				.Select(grouping => grouping.OrderBy<ITrackLinkingInfo<TrackT>>(ITrackLinkingInfo.TrackOrderWithinAlbums));
			return groupedTracks.SelectMany(albumGroup => GroupTracksIntoWorksWithinAlbums(albumGroup));
			
		}

		private IEnumerable<IGrouping<WorkNameKey, ITrackLinkingInfo<TrackT>>> GroupTracksIntoWorksWithinAlbums(IEnumerable<ITrackLinkingInfo<TrackT>> trackMetadata)
		{
			var wrappedLinkerChoices = trackMetadata.Select(track => (track, _workNameLinker.GetWorkNameForTrack(track))).ToArray();
			var indicesByLengthOrder = Enumerable.Range(0, wrappedLinkerChoices.Length).OrderBy(index => wrappedLinkerChoices[index].Item2.Length).Reverse();
			indicesByLengthOrder.Each(index =>
			{
				var originalWorkName = wrappedLinkerChoices[index].Item2;
				var newWorkName = originalWorkName;
				string neighborWorkName;
				if (index > 0 && newWorkName.Contains(neighborWorkName = wrappedLinkerChoices[index - 1].Item2))
					newWorkName = neighborWorkName;
				if (index < wrappedLinkerChoices.Length - 1 && newWorkName.Contains(neighborWorkName = wrappedLinkerChoices[index + 1].Item2))
					newWorkName = neighborWorkName;
				if (!Equals(originalWorkName, newWorkName))
					wrappedLinkerChoices[index] = (wrappedLinkerChoices[index].track, newWorkName);
			});
			return wrappedLinkerChoices.GroupBy(pair => new WorkNameKey(pair.Item2, pair.track.AlbumName, pair.track.AlbumUri), pair => pair.track);
		}
	}
}

