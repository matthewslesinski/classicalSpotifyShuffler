using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpotifyProject.Utils;
using CustomResources.Utils.Extensions;
using ApplicationResources.Logging;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackSetters
{
	internal interface IAddTracksOperation : IPlaylistModification
	{
		async Task<string> IPlaylistModification.SendRequest(ISpotifyAccessor spotifyAccessor, string playlistId, string previousSnapshotId)
		{
			Logger.Verbose($"Adding {UrisToAdd.Count} tracks to playlist with Id {playlistId}{(AddPosition.HasValue ? " at position " + AddPosition : "")}");
			var response = await spotifyAccessor.AddPlaylistItems(playlistId, UrisToAdd, AddPosition).WithoutContextCapture();
			return response.SnapshotId;
		}

		int? AddPosition { get; }
		IList<string> UrisToAdd { get; }
	}

	internal interface IRemoveTracksOperation : IPlaylistModification
	{
		async Task<string> IPlaylistModification.SendRequest(ISpotifyAccessor spotifyAccessor, string playlistId, string previousSnapshotId)
		{
			Logger.Verbose($"Removing {UrisToRemove.Count} tracks from playlist with Id {playlistId}");
			var response = await spotifyAccessor.RemovePlaylistItems(playlistId, previousSnapshotId, UrisToRemove).WithoutContextCapture();
			return response.SnapshotId;
		}

		IList<string> UrisToRemove { get; }
	}

	internal interface IReorderTracksOperation : IPlaylistModification
	{
		async Task<string> IPlaylistModification.SendRequest(ISpotifyAccessor spotifyAccessor, string playlistId, string previousSnapshotId)
		{
			Logger.Verbose($"Moving {RangeLength} tracks in playlist with Id {playlistId} from {RangeStartIndex} to {ToInsertIndex}");
			var response = await spotifyAccessor.ReorderPlaylistItems(playlistId, previousSnapshotId, RangeStartIndex, RangeLength, ToInsertIndex).WithoutContextCapture();
			return response.SnapshotId;
		}

		int RangeStartIndex { get; }
		int RangeLength { get; }
		int ToInsertIndex { get; }
	}

	internal interface IReplaceTracksOperation : IPlaylistModification
	{
		async Task<string> IPlaylistModification.SendRequest(ISpotifyAccessor spotifyAccessor, string playlistId, string previousSnapshotId)
		{
			Logger.Verbose($"Overwriting existing tracks from playlist with Id {playlistId} with {UrisToReplaceWith.Count} new tracks");
			await spotifyAccessor.ReplacePlaylistItems(playlistId, UrisToReplaceWith).WithoutContextCapture();
			return null;
		}

		List<string> UrisToReplaceWith { get; }
	}

	internal interface IGetCurrentSnapshotIdOperation : IPlaylistModification
	{
		async Task<string> IPlaylistModification.SendRequest(ISpotifyAccessor spotifyAccessor, string playlistId, string previousSnapshotId)
		{
			Logger.Verbose($"Getting snapshot ID for playlist with Id {playlistId}");
			var response = await spotifyAccessor.GetPlaylist(playlistId, new[] { "snapshot_id" }).WithoutContextCapture();
			return response.SnapshotId;
		}
	}

	internal class AddOperation : IAddTracksOperation
	{
		internal AddOperation(IEnumerable<string> uris, int? addPosition = null)
		{
			if (uris.Count() > SpotifyConstants.PlaylistRequestBatchSize)
				throw new ArgumentException($"{uris.Count()} is too many tracks to add to a playlist at once");
			UrisToAdd = uris.ToList();
			AddPosition = addPosition;
		}

		public int? AddPosition { get; }

		public IList<string> UrisToAdd { get; }

		public static IEnumerable<IPlaylistModification> CreateOperations(IEnumerable<string> uris)
		{
			return uris.Batch(SpotifyConstants.PlaylistRequestBatchSize).Select(batch => new AddOperation(batch));
		}
	}

	internal class RemoveOperation : IRemoveTracksOperation
	{
		internal RemoveOperation(IEnumerable<string> uris)
		{
			UrisToRemove = uris.ToList();
			if (UrisToRemove.Count() > SpotifyConstants.PlaylistRequestBatchSize)
				throw new ArgumentException($"{UrisToRemove.Count()} is too many tracks to remove from a playlist at once");
		}

		public IList<string> UrisToRemove { get; }

		public static IEnumerable<IPlaylistModification> CreateOperations(IEnumerable<string> uris)
		{
			return uris.Batch(SpotifyConstants.PlaylistRequestBatchSize).Select(batch => new RemoveOperation(batch));
		}
	}

	internal class ReorderOperation : IReorderTracksOperation
	{
		internal ReorderOperation(int rangeStart, int rangeLength, int toInsert)
		{
			RangeStartIndex = rangeStart;
			RangeLength = rangeLength;
			ToInsertIndex = toInsert;
		}

		public int RangeStartIndex { get; }

		public int RangeLength { get; }

		public int ToInsertIndex { get; }
	}

	internal class ReplaceOperation : IReplaceTracksOperation
	{
		internal ReplaceOperation(IEnumerable<string> uris = null)
		{
			if (uris?.Count() > SpotifyConstants.PlaylistRequestBatchSize)
				throw new ArgumentException($"{uris.Count()} is too many tracks to put in a playlist at once");
			UrisToReplaceWith = uris?.ToList() ?? new List<string>();
		}

		public List<string> UrisToReplaceWith { get; }
	}

	internal class GetCurrentSnapshotIdOperation : IGetCurrentSnapshotIdOperation
	{ }
}
