using System;
using CustomResources.Utils.Concepts.DataStructures;
using SpotifyAPI.Web;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using SpotifyProject.SpotifyUtils;

namespace ClassicalSpotifyShuffler.Common
{
	public interface ISpotifyItemDisplay
	{
		string Title { get; }
		string? UnderlyingUri { get; }
		string Caption { get; }
		PlaybackContextType? ContextType { get; }
	}

	public interface IContextDisplay<ContainedTrackT> : ISpotifyItemDisplay
	{
		IQueryCache<ContainedTrackT> ContainedTracks { get; }
	}

	public class AlbumContextDisplay : IContextDisplay<SimpleTrack>
	{
		public AlbumContextDisplay(SavedAlbum album) : this(album.Album)
		{ }

		public AlbumContextDisplay(FullAlbum album)
		{
			Title = album.Name;
			ContextType = PlaybackContextType.Album;
			UnderlyingUri = album.Uri;
			Caption = string.Join(", ", album.Artists.Select(artist => artist.Name));
			ContainedTracks = new AlbumTracksCache(album.Id, loadType: LoadType.Lazy, firstPage: album.Tracks);
		}

		public AlbumContextDisplay(SimpleAlbum album)
		{
			Title = album.Name;
			ContextType = PlaybackContextType.Album;
			UnderlyingUri = album.Uri;
			Caption = string.Join(", ", album.Artists.Select(artist => artist.Name));
			ContainedTracks = new AlbumTracksCache(album.Id, loadType: LoadType.Lazy);
		}

		public string Title { get; }
		public string? UnderlyingUri { get; }
		public string Caption { get; }
		public PlaybackContextType? ContextType { get; }
		public IQueryCache<SimpleTrack> ContainedTracks { get; }
	}

	public class PlaylistContextDisplay : IContextDisplay<FullTrack>
	{
		public PlaylistContextDisplay(SimplePlaylist playlist)
		{
			Title = playlist.Name;
			ContextType = PlaybackContextType.Playlist;
			UnderlyingUri = playlist.Uri;
			Caption = $"{(playlist.Tracks?.Total != null ? playlist.Tracks.Total : "unknown number of")} tracks";
			ContainedTracks = new PlaylistTracksCache(playlist.Id, loadType: LoadType.Lazy, firstPage: playlist.Tracks);
		}

		public string Title { get; }
		public string? UnderlyingUri { get; }
		public string Caption { get; }
		public PlaybackContextType? ContextType { get; }
		public IQueryCache<FullTrack> ContainedTracks { get; }
	}

	public class AllLikedTracksContextDisplay : IContextDisplay<FullTrack>
	{
		public AllLikedTracksContextDisplay()
		{
			Title = "All Liked Tracks";
			ContextType = PlaybackContextType.AllLikedTracks;
			UnderlyingUri = null;
			ContainedTracks = new AllLikedTracksCache(loadType: LoadType.PartiallyOnInitialization);
		}

		public string Title { get; }
		public string? UnderlyingUri { get; }
		public string Caption {
			get
			{
				var task = ContainedTracks.GetTotalCount();
				return $"{(task.IsCompletedSuccessfully ? task.Result : "unknown number of")} tracks";
			}
		} 
		public PlaybackContextType? ContextType { get; }
		public IQueryCache<FullTrack> ContainedTracks { get; }
	}

	public class TrackDisplay : ISpotifyItemDisplay
	{
		public TrackDisplay(FullTrack track)
		{
			Title = track.Name;
			UnderlyingUri = track.Uri;
			Caption = string.Join(", ", track.Artists.Select(artist => artist.Name));
			ContextType = null;
		}

		public string Title { get; }
		public string? UnderlyingUri { get; }
		public string Caption { get; }
		public PlaybackContextType? ContextType { get; }
	}

	public record MiscContextDisplay(string Title, string? UnderlyingUri, string Caption, PlaybackContextType? ContextType) : ISpotifyItemDisplay;
}