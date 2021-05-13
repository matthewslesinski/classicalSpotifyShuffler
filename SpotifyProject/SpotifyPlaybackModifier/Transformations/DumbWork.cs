using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using SpotifyAPI.Web;
using SpotifyProject.Utils;

namespace SpotifyProject.SpotifyPlaybackModifier.Transformations
{
    public class DumbWork<TrackT> : ITrackGrouping<int, TrackT>
    {
        public int Id { get; }
        public IEnumerable<TrackT> Tracks { get; }

        public int Key => Id;

        public DumbWork(int id, IEnumerable<TrackT> tracks)
        {
            Id = id;
            Tracks = tracks;
        }

        public override bool Equals(object obj)
        {
            return obj is DumbWork<TrackT> o
                   && Equals(Id, o.Id)
                   && Tracks.Select(ReflectionUtils<TrackT>.RetrieveGetterByPropertyName<string>(nameof(SimpleTrack.Uri)))
                       .SequenceEqual(o.Tracks.Select(ReflectionUtils<TrackT>.RetrieveGetterByPropertyName<string>(nameof(SimpleTrack.Uri))));
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public override string ToString()
        {
            return $"Group {Id}, {Tracks.Count()} tracks: [{string.Join("; ", Tracks.Select(ReflectionUtils<TrackT>.RetrieveGetterByPropertyName<int>(nameof(SimpleTrack.Name))))}]";
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
