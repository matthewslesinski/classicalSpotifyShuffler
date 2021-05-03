using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SpotifyAPI.Web;
using SpotifyProject.Utils;

namespace SpotifyProject.SpotifyPlaybackModifier.Transformations
{
	public class SimpleWork<TrackT> : ITrackGrouping<(string name, string albumName), TrackT>
	{
		public string Name { get; }
		public string AlbumName { get; }
		public IEnumerable<TrackT> Tracks { get; }

		public (string name, string albumName) Key => (Name, AlbumName);

		public SimpleWork(string name, string albumName, IEnumerable<TrackT> tracks)
		{
			AlbumName = albumName;
			Name = name;
			Tracks = tracks;
		}

		public override bool Equals(object obj)
		{
			return obj is SimpleWork<TrackT> o
				&& Equals(Name, o.Name)
				&& Equals(AlbumName, o.AlbumName)
				&& Tracks.Select(ReflectionUtils<TrackT>.RetrieveGetterByPropertyName<string>(nameof(SimpleTrack.Uri)))
				.SequenceEqual(o.Tracks.Select(ReflectionUtils<TrackT>.RetrieveGetterByPropertyName<string>(nameof(SimpleTrack.Uri))));
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
