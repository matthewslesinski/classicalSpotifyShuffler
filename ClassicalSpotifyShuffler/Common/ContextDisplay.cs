using System;
using CustomResources.Utils.Concepts.DataStructures;
using SpotifyAPI.Web;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;
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

	public interface IContextDisplay : ISpotifyItemDisplay
	{
		IQueryCache<IPlayableTrackLinkingInfo> ContainedTracks { get; }
	}

	public class AlbumContextDisplay : IContextDisplay
	{
		public AlbumContextDisplay(SavedAlbum album) : this(album.Album)
		{ }

		public AlbumContextDisplay(FullAlbum album)
		{
			Title = album.Name;
			ContextType = PlaybackContextType.Album;
			UnderlyingUri = album.Uri;
			Caption = string.Join(", ", album.Artists.Select(artist => artist.Name));
			ContainedTracks = new AlbumTracksCache(album, loadType: LoadType.Lazy, firstPage: album.Tracks);
		}

		public AlbumContextDisplay(SimpleAlbum album)
		{
			Title = album.Name;
			ContextType = PlaybackContextType.Album;
			UnderlyingUri = album.Uri;
			Caption = string.Join(", ", album.Artists.Select(artist => artist.Name));
			ContainedTracks = new AlbumTracksCache(album, loadType: LoadType.Lazy);
		}

		public string Title { get; }
		public string? UnderlyingUri { get; }
		public string Caption { get; }
		public PlaybackContextType? ContextType { get; }
		public IQueryCache<IPlayableTrackLinkingInfo> ContainedTracks { get; }

		public override bool Equals(object? obj)
		{
			return obj is AlbumContextDisplay display &&
				   UnderlyingUri == display.UnderlyingUri &&
				   ContextType == display.ContextType;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(UnderlyingUri, ContextType);
		}
	}

	public class PlaylistContextDisplay : IContextDisplay
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
		public IQueryCache<IPlayableTrackLinkingInfo> ContainedTracks { get; }

		public override bool Equals(object? obj)
		{
			return obj is PlaylistContextDisplay display &&
				   UnderlyingUri == display.UnderlyingUri &&
				   ContextType == display.ContextType;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(UnderlyingUri, ContextType);
		}
	}

	public class AllLikedTracksContextDisplay : IContextDisplay
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
		public IQueryCache<IPlayableTrackLinkingInfo> ContainedTracks { get; }

		public override bool Equals(object? obj)
		{
			return obj is AllLikedTracksContextDisplay display &&
				   ContextType == display.ContextType;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(ContextType);
		}
	}

	public class TrackDisplay : ISpotifyItemDisplay
	{
		public TrackDisplay(IPlayableTrackLinkingInfo track, int trackNumber)
		{
			Title = track.Name;
			UnderlyingUri = track.Uri;
			Caption = string.Join(", ", track.ArtistNames);
			ContextType = null;
			TrackNumber = trackNumber;
		}

		public string Title { get; }
		public string UnderlyingUri { get; }
		public string Caption { get; }
		public PlaybackContextType? ContextType { get; }
		public int TrackNumber { get; }

		public override bool Equals(object? obj)
		{
			return obj is TrackDisplay display &&
				   UnderlyingUri == display.UnderlyingUri;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(UnderlyingUri);
		}
	}

	public record MiscContextDisplay(string Title, string? UnderlyingUri, string Caption, PlaybackContextType? ContextType, IQueryCache<IPlayableTrackLinkingInfo> ContainedTracks) : IContextDisplay;
}