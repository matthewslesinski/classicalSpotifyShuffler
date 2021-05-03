using System;
using SpotifyAPI.Web;

namespace SpotifyProject.SpotifyPlaybackModifier.TrackLinking
{
	public interface ITrackLinkingInfo
	{
		string Name { get; }
		string AlbumName { get; }
		string Uri { get; }
		(int discNumber, int trackNumber) AlbumIndex { get; }
	}

	public interface ITrackLinkingInfo<TrackT> : ITrackLinkingInfo
	{
		TrackT OriginalTrack { get; }
	}

	public interface ISimpleTrackLinkingInfo : ITrackLinkingInfo<SimpleTrack> {
		string ITrackLinkingInfo.Name => OriginalTrack.Name;
		string ITrackLinkingInfo.Uri => OriginalTrack.Uri;
		(int discNumber, int trackNumber) ITrackLinkingInfo.AlbumIndex => (OriginalTrack.DiscNumber, OriginalTrack.TrackNumber);
	}

	public interface IFullTrackLinkingInfo : ITrackLinkingInfo<FullTrack>
	{
		string ITrackLinkingInfo.Name => OriginalTrack.Name;
		string ITrackLinkingInfo.Uri => OriginalTrack.Uri;
		string ITrackLinkingInfo.AlbumName => OriginalTrack.Album.Name;
		(int discNumber, int trackNumber) ITrackLinkingInfo.AlbumIndex => (OriginalTrack.TrackNumber, OriginalTrack.TrackNumber);
	}

}
