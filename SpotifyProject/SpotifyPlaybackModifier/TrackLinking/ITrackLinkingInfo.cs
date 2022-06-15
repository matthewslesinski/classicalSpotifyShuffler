using System;
using System.Collections.Generic;
using System.Linq;
using SpotifyAPI.Web;
using CustomResources.Utils.Concepts;
using CustomResources.Utils.Extensions;

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

		static readonly IEqualityComparer<ITrackLinkingInfo> EqualityByUris = new KeyBasedEqualityComparer<ITrackLinkingInfo, string>(track => track.Uri);

		static readonly IComparer<ITrackLinkingInfo> TrackOrderWithinAlbums =
			ComparerUtils.ComparingBy<ITrackLinkingInfo, (int discNumber, int trackNumber)>(t => t.AlbumIndex,
				ComparerUtils.ComparingBy<(int discNumber, int trackNumber)>(i => i.discNumber).ThenBy(i => i.trackNumber))
					.ThenBy(t => t.Name);
	}

	public interface ITrackLinkingInfo<TrackT> : ITrackLinkingInfo
	{
		TrackT OriginalTrack { get; }
	}

	public interface IPlayableTrackLinkingInfo : ITrackLinkingInfo
	{
		bool IsPlayable { get; }
		bool TryGetLinkedTrack(out IUnplayableTrackLinkingInfo linkedTrack);
		ITrackLinkingInfo GetOriginallyRequestedVersion() => TryGetLinkedTrack(out var linkedTrack) ? linkedTrack : this;
	}

	public interface IPlayableTrackLinkingInfo<TrackT> : ITrackLinkingInfo<TrackT>, IPlayableTrackLinkingInfo { }

	public interface ISimpleTrackLinkingInfo : IPlayableTrackLinkingInfo<SimpleTrack>
	{
		string ITrackLinkingInfo.Name => OriginalTrack.Name;
		string ITrackLinkingInfo.Uri => OriginalTrack.Uri;
		bool ITrackLinkingInfo.IsLocal => false;
		int ITrackLinkingInfo.DurationMs => OriginalTrack.DurationMs;
		IEnumerable<string> ITrackLinkingInfo.ArtistNames => OriginalTrack.Artists.Select(artist => artist.Name);
		(int discNumber, int trackNumber) ITrackLinkingInfo.AlbumIndex => (OriginalTrack.DiscNumber, OriginalTrack.TrackNumber);
		bool IPlayableTrackLinkingInfo.IsPlayable => OriginalTrack.IsPlayable;
		bool IPlayableTrackLinkingInfo.TryGetLinkedTrack(out IUnplayableTrackLinkingInfo linkedTrack)
		{
			if (OriginalTrack.LinkedFrom != null)
			{
				linkedTrack = new UnplayableTrackWrapper(OriginalTrack.LinkedFrom, this);
				return true;
			}
			linkedTrack = null;
			return false;
		}
	}

	public interface IFullTrackLinkingInfo : IPlayableTrackLinkingInfo<FullTrack>
	{
		string ITrackLinkingInfo.Name => OriginalTrack.Name;
		string ITrackLinkingInfo.Uri => OriginalTrack.Uri;
		string ITrackLinkingInfo.AlbumName => OriginalTrack.Album.Name;
		string ITrackLinkingInfo.AlbumUri => OriginalTrack.Album.Uri;
		bool ITrackLinkingInfo.IsLocal => OriginalTrack.IsLocal;
		int ITrackLinkingInfo.DurationMs => OriginalTrack.DurationMs;
		IEnumerable<string> ITrackLinkingInfo.ArtistNames => OriginalTrack.Artists.Select(artist => artist.Name);
		(int discNumber, int trackNumber) ITrackLinkingInfo.AlbumIndex => (OriginalTrack.DiscNumber, OriginalTrack.TrackNumber);
		bool IPlayableTrackLinkingInfo.IsPlayable => OriginalTrack.IsPlayable;
		bool IPlayableTrackLinkingInfo.TryGetLinkedTrack(out IUnplayableTrackLinkingInfo linkedTrack)
		{
			if (OriginalTrack.LinkedFrom != null)
			{
				linkedTrack = new UnplayableTrackWrapper(OriginalTrack.LinkedFrom, this);
				return true;
			}
			linkedTrack = null;
			return false;
		}
	}

	public interface IUnplayableTrackLinkingInfo : ITrackLinkingInfo<LinkedTrack>
	{
		string ITrackLinkingInfo.Name => PlayableVersion.Name;
		string ITrackLinkingInfo.Uri => OriginalTrack.Uri;
		string ITrackLinkingInfo.AlbumName => PlayableVersion.AlbumName;
		string ITrackLinkingInfo.AlbumUri => PlayableVersion.AlbumUri;
		bool ITrackLinkingInfo.IsLocal => PlayableVersion.IsLocal;
		int ITrackLinkingInfo.DurationMs => PlayableVersion.DurationMs;
		IEnumerable<string> ITrackLinkingInfo.ArtistNames => PlayableVersion.ArtistNames;
		(int discNumber, int trackNumber) ITrackLinkingInfo.AlbumIndex => PlayableVersion.AlbumIndex;

		IPlayableTrackLinkingInfo PlayableVersion { get; }
	}

	public interface ITrackLinkingInfoWrapper<InfoT> : IPlayableTrackLinkingInfo<InfoT>
		where InfoT : IPlayableTrackLinkingInfo
	{
		string ITrackLinkingInfo.Name => OriginalTrack.Name;
		string ITrackLinkingInfo.Uri => OriginalTrack.Uri;
		string ITrackLinkingInfo.AlbumName => OriginalTrack.AlbumName;
		string ITrackLinkingInfo.AlbumUri => OriginalTrack.AlbumUri;
		bool ITrackLinkingInfo.IsLocal => OriginalTrack.IsLocal;
		int ITrackLinkingInfo.DurationMs => OriginalTrack.DurationMs;
		IEnumerable<string> ITrackLinkingInfo.ArtistNames => OriginalTrack.ArtistNames;
		(int discNumber, int trackNumber) ITrackLinkingInfo.AlbumIndex => OriginalTrack.AlbumIndex;
		bool IPlayableTrackLinkingInfo.IsPlayable => OriginalTrack.IsPlayable;
		bool IPlayableTrackLinkingInfo.TryGetLinkedTrack(out IUnplayableTrackLinkingInfo linkedTrack) => OriginalTrack.TryGetLinkedTrack(out linkedTrack);
	}

	public static class ITrackLinkingInfoExtensions
	{
		public static string ToDescriptiveString(this ITrackLinkingInfo track) =>
			ToDescriptiveString((track.Uri, track.AlbumName, track.AlbumIndex.discNumber, track.AlbumIndex.trackNumber, track.Name));

		public static string ToDescriptiveString(this (string uri, string albumName, int discNumber, int trackNumber, string trackName) trackInfo) =>
			$"(uri: {trackInfo.uri} | albumIndex: {trackInfo.albumName}:{trackInfo.discNumber}.{trackInfo.trackNumber} | name: {trackInfo.trackName})";
	}
}
