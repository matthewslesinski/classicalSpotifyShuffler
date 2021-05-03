using System;
using System.Collections.Generic;
using SpotifyAPI.Web;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;
using SpotifyProject.Utils;

namespace SpotifyProject.SpotifyPlaybackModifier.Transformations
{
	public class SimpleWorkShuffle<InputContextT, OutputContextT, TrackT> : IGroupingPlaybackTransformation<InputContextT, OutputContextT, TrackT, (string trackName, string albumName)>
		where InputContextT : ISpotifyPlaybackContext<TrackT> where OutputContextT : IReorderedPlaybackContext<TrackT, InputContextT>
	{
		private readonly Func<InputContextT, IEnumerable<TrackT>, OutputContextT> _contextConstructor;
		private readonly ITrackLinker<InputContextT, TrackT, (string trackName, string albumName)> _trackLinker;

		public SimpleWorkShuffle(Func<InputContextT, IEnumerable<TrackT>, OutputContextT> contextConstructor, ITrackLinker<InputContextT, TrackT, (string trackName, string albumName)> trackLinker)
		{
			_contextConstructor = contextConstructor;
			_trackLinker = trackLinker;
		}

		public OutputContextT ConstructNewContext(InputContextT inputContext, IEnumerable<TrackT> newTrackOrder)
		{
			return _contextConstructor(inputContext, newTrackOrder);
		}

		public IEnumerable<ITrackGrouping<(string trackName, string albumName), TrackT>> GroupTracksIntoWorks(InputContextT playbackContext, IEnumerable<TrackT> tracks)
		{
			return _trackLinker.GroupTracksIntoWorks(playbackContext, tracks);
		}

		public IEnumerable<ITrackGrouping<(string trackName, string albumName), TrackT>> ReorderWorks(IEnumerable<ITrackGrouping<(string trackName, string albumName), TrackT>> works)
		{
			return works.RandomShuffle();
		}
	}
}
