using System;
using SpotifyAPI.Web;

namespace SpotifyProject.SpotifyPlaybackModifier.TrackLinking
{
	public class FullTrackWrapper : IFullTrackLinkingInfo
	{
		public FullTrackWrapper(FullTrack track)
		{
			OriginalTrack = track;
		}

		public FullTrack OriginalTrack { get; }

		public override string ToString() => this.ToDescriptiveString();
	}

	public class SimpleTrackAndAlbumWrapper : ISimpleTrackLinkingInfo
	{
		private readonly string _containingAlbumName;
		private readonly string _containingAlbumUri;

		public SimpleTrackAndAlbumWrapper(SimpleTrack track, SimpleAlbum containingAlbum) : this(track, containingAlbum.Name, containingAlbum.Uri)
		{
		}

		public SimpleTrackAndAlbumWrapper(SimpleTrack track, FullAlbum containingAlbum) : this(track, containingAlbum.Name, containingAlbum.Uri)
		{
		}

		public SimpleTrackAndAlbumWrapper(SimpleTrack track, string albumName, string albumUri)
		{
			_containingAlbumName = albumName;
			_containingAlbumUri = albumUri;
			OriginalTrack = track;
		}

		public SimpleTrack OriginalTrack { get; }
		public string AlbumName => _containingAlbumName;
		public string AlbumUri => _containingAlbumUri;

		public override string ToString() => this.ToDescriptiveString();
	}

	public class TrackLinkingInfoWrapper<TrackT, InfoT> : ITrackLinkingInfoWrapper<TrackT, InfoT>
		where InfoT : ITrackLinkingInfo<TrackT>
	{
		public TrackLinkingInfoWrapper(InfoT trackInfo)
		{
			OriginalTrack = trackInfo;
		}

		public InfoT OriginalTrack { get; }

		public override string ToString() => OriginalTrack.ToDescriptiveString();
	}
}
