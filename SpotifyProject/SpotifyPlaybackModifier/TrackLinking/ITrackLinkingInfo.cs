using System;
using System.Collections.Generic;
using System.Linq;
using SpotifyAPI.Web;

namespace SpotifyProject.SpotifyPlaybackModifier.TrackLinking
{
	public interface ITrackLinkingInfo
	{
		string Name { get; }
		string AlbumName { get; }
		string Uri { get; }
		bool IsLocal { get; }
		int DurationMs { get; }
		IEnumerable<string> ArtistNames { get; }
		(int discNumber, int trackNumber) AlbumIndex { get; }
	}

	public interface ITrackLinkingInfo<TrackT> : ITrackLinkingInfo
	{
		TrackT OriginalTrack { get; }
	}

	public interface ISimpleTrackLinkingInfo : ITrackLinkingInfo<SimpleTrack> {
		string ITrackLinkingInfo.Name => OriginalTrack.Name;
		string ITrackLinkingInfo.Uri => OriginalTrack.Uri;
		bool ITrackLinkingInfo.IsLocal => false;
		int ITrackLinkingInfo.DurationMs => OriginalTrack.DurationMs;
		IEnumerable<string> ITrackLinkingInfo.ArtistNames => OriginalTrack.Artists.Select(artist => artist.Name);
		(int discNumber, int trackNumber) ITrackLinkingInfo.AlbumIndex => (OriginalTrack.DiscNumber, OriginalTrack.TrackNumber);
	}

	public interface IFullTrackLinkingInfo : ITrackLinkingInfo<FullTrack>
	{
		string ITrackLinkingInfo.Name => OriginalTrack.Name;
		string ITrackLinkingInfo.Uri => OriginalTrack.Uri;
		string ITrackLinkingInfo.AlbumName => OriginalTrack.Album.Name;
		bool ITrackLinkingInfo.IsLocal => OriginalTrack.IsLocal;
		int ITrackLinkingInfo.DurationMs => OriginalTrack.DurationMs;
		IEnumerable<string> ITrackLinkingInfo.ArtistNames => OriginalTrack.Artists.Select(artist => artist.Name);
		(int discNumber, int trackNumber) ITrackLinkingInfo.AlbumIndex => (OriginalTrack.TrackNumber, OriginalTrack.TrackNumber);
	}

}
