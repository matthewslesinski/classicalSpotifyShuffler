using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;

namespace SpotifyProject.SpotifyPlaybackModifier.Transformations
{
	public class SimpleWork<TrackT> : ITrackGrouping<WorkNameKey, TrackT>
	{
		public string Name { get; }
		public string AlbumName { get; }
		public string AlbumUri { get; }
		public IEnumerable<TrackT> Tracks => _trackLinkingInfos.Select(trackMetaData => trackMetaData.OriginalTrack);
		private readonly IEnumerable<ITrackLinkingInfo<TrackT>> _trackLinkingInfos;
		public WorkNameKey Key => new (Name, AlbumName, AlbumUri);

		public SimpleWork(string name, string albumName, string albumUri, IEnumerable<ITrackLinkingInfo<TrackT>> tracks)
		{
			AlbumName = albumName;
			AlbumUri = albumUri;
			Name = name;
			_trackLinkingInfos = tracks;
		}

		public override bool Equals(object obj)
		{
			return obj is SimpleWork<TrackT> o
				&& Equals(Name, o.Name)
				&& Equals(AlbumUri, o.AlbumUri)
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
