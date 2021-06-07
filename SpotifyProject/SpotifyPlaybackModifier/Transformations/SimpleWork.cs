using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SpotifyAPI.Web;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;
using SpotifyProject.Utils;

namespace SpotifyProject.SpotifyPlaybackModifier.Transformations
{
	public class SimpleWork<TrackT> : ITrackGrouping<(string name, string albumName), TrackT>
	{
		public string Name { get; }
		public string AlbumName { get; }
		public IEnumerable<TrackT> Tracks => _trackLinkingInfos.Select(trackMetaData => trackMetaData.OriginalTrack);
		private readonly IEnumerable<ITrackLinkingInfo<TrackT>> _trackLinkingInfos;
		public (string name, string albumName) Key => (Name, AlbumName);

		public SimpleWork(string name, string albumName, IEnumerable<ITrackLinkingInfo<TrackT>> tracks)
		{
			AlbumName = albumName;
			Name = name;
			_trackLinkingInfos = tracks;
		}

		public override bool Equals(object obj)
		{
			return obj is SimpleWork<TrackT> o
				&& Equals(Name, o.Name)
				&& Equals(AlbumName, o.AlbumName)
				&& _trackLinkingInfos.Select(trackInfo => trackInfo.Uri)
						.SequenceEqual(o._trackLinkingInfos.Select(trackInfo => trackInfo.Uri));
		}

		public override int GetHashCode()
		{
			return Key.GetHashCode();
		}

		public override string ToString()
		{
			return $"{Name} ({AlbumName})";
		}

		public IEnumerator<TrackT> GetEnumerator()
		{
			return Tracks.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return Tracks.GetEnumerator();
		}
	}
}
