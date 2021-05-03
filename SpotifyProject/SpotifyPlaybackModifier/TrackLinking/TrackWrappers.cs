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
	}

	public class SimpleTrackAndAlbumWrapper : ISimpleTrackLinkingInfo
	{
		private readonly string _containingAlbumName;

		public SimpleTrackAndAlbumWrapper(SimpleTrack track, SimpleAlbum containingAlbum) : this(track, containingAlbum.Name)
		{
		}

		public SimpleTrackAndAlbumWrapper(SimpleTrack track, FullAlbum containingAlbum) : this(track, containingAlbum.Name)
		{
		}

		public SimpleTrackAndAlbumWrapper(SimpleTrack track, string albumName)
		{
			_containingAlbumName = albumName;
			OriginalTrack = track;
		}

		public SimpleTrack OriginalTrack { get; }
		public string AlbumName => _containingAlbumName;
	}
}
