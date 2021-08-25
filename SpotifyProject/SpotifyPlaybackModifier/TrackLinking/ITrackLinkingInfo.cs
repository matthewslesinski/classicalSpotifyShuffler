using System;
using System.Collections.Generic;
using System.Linq;
using SpotifyAPI.Web;
using SpotifyProject.Utils.Concepts;
using SpotifyProject.Utils.Extensions;

namespace SpotifyProject.SpotifyPlaybackModifier.TrackLinking
{
	public interface ITrackLinkingInfo
	{
		string Name { get; }
		string AlbumName { get; }
		string Uri { get; }
		string AlbumUri { get; }
		bool IsLocal { get; }
		int DurationMs { get; }
		IEnumerable<string> ArtistNames { get; }
		(int discNumber, int trackNumber) AlbumIndex { get; }

		static readonly IEqualityComparer<ITrackLinkingInfo> EqualityDefinition = new SimpleEqualityComparer<ITrackLinkingInfo>(
			(t1, t2) => Equals(t1.Uri, t2.Uri) || (Equals(t1.Name, t2.Name) && Equals(t1.AlbumName, t2.AlbumName) && Equals(t1.AlbumIndex, t2.AlbumIndex) && t1.ArtistNames.ContainsSameElements(t2.ArtistNames)),
			t => HashCode.Combine(t.Name, t.AlbumUri, t.AlbumIndex));

		static readonly IComparer<ITrackLinkingInfo> TrackOrderWithinAlbums =
			ComparerUtils.ComparingBy<ITrackLinkingInfo, (int discNumber, int trackNumber)>(t => t.AlbumIndex,
				ComparerUtils.ComparingBy<(int discNumber, int trackNumber)>(i => i.discNumber).ThenBy(i => i.trackNumber));
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
		string ITrackLinkingInfo.AlbumUri => OriginalTrack.Album.Uri;
		bool ITrackLinkingInfo.IsLocal => OriginalTrack.IsLocal;
		int ITrackLinkingInfo.DurationMs => OriginalTrack.DurationMs;
		IEnumerable<string> ITrackLinkingInfo.ArtistNames => OriginalTrack.Artists.Select(artist => artist.Name);
		(int discNumber, int trackNumber) ITrackLinkingInfo.AlbumIndex => (OriginalTrack.DiscNumber, OriginalTrack.TrackNumber);
	}

	public interface ITrackLinkingInfoWrapper<OriginalTrackT, InfoT> : ITrackLinkingInfo<InfoT>
		where InfoT : ITrackLinkingInfo<OriginalTrackT>
	{
		string ITrackLinkingInfo.Name => OriginalTrack.Name;
		string ITrackLinkingInfo.Uri => OriginalTrack.Uri;
		string ITrackLinkingInfo.AlbumName => OriginalTrack.AlbumName;
		string ITrackLinkingInfo.AlbumUri => OriginalTrack.AlbumUri;
		bool ITrackLinkingInfo.IsLocal => OriginalTrack.IsLocal;
		int ITrackLinkingInfo.DurationMs => OriginalTrack.DurationMs;
		IEnumerable<string> ITrackLinkingInfo.ArtistNames => OriginalTrack.ArtistNames;
		(int discNumber, int trackNumber) ITrackLinkingInfo.AlbumIndex => OriginalTrack.AlbumIndex;
	}

	public static class ITrackLinkingInfoExtensions
	{
		public static string ToDescriptiveString(this ITrackLinkingInfo track) =>
			ToDescriptiveString((track.Uri, track.AlbumName, track.AlbumIndex.discNumber, track.AlbumIndex.trackNumber, track.Name));

		public static string ToDescriptiveString(this (string uri, string albumName, int discNumber, int trackNumber, string trackName) trackInfo) =>
			$"(uri: {trackInfo.uri} | albumIndex: {trackInfo.albumName}:{trackInfo.discNumber}.{trackInfo.trackNumber} | name: {trackInfo.trackName})";
	}
}
