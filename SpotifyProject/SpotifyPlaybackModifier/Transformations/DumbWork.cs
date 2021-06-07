using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using SpotifyAPI.Web;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;
using SpotifyProject.Utils;

namespace SpotifyProject.SpotifyPlaybackModifier.Transformations
{
    public class DumbWork<TrackT> : ITrackGrouping<int, TrackT>
    {
        public int Id { get; }
        public IEnumerable<TrackT> Tracks => _trackLinkingInfos.Select(trackMetaData => trackMetaData.OriginalTrack);
        private readonly IEnumerable<ITrackLinkingInfo<TrackT>> _trackLinkingInfos;

        public int Key => Id;

        public DumbWork(int id, IEnumerable<ITrackLinkingInfo<TrackT>> tracks)
        {
            Id = id;
            _trackLinkingInfos = tracks;
        }

        public override bool Equals(object obj)
        {
            return obj is DumbWork<TrackT> o
                   && Equals(Id, o.Id)
                   && _trackLinkingInfos.Select(trackInfo => trackInfo.Uri)
                        .SequenceEqual(o._trackLinkingInfos.Select(trackInfo => trackInfo.Uri));
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public override string ToString()
        {
            return $"Group {Id}, {_trackLinkingInfos.Count()} tracks: [{string.Join("; ", _trackLinkingInfos.Select(trackInfo => trackInfo.Name))}]";
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
