using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using System.Linq;
using CustomResources.Utils.Algorithms;
using System.Diagnostics.CodeAnalysis;
using CustomResources.Utils.Concepts;
using CustomResources.Utils.Extensions;
using SpotifyProject.Utils;
using ApplicationResources.Logging;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackSetters
{
	public class EfficientPlaylistTrackModifier : SpotifyAccessorBase, IPlaylistTrackModifier
	{
		public EfficientPlaylistTrackModifier(SpotifyConfiguration spotifyConfiguration) : base(spotifyConfiguration)
		{ }

		private static bool ShouldIncludeNonLocalTrack(ITrackLinkingInfo track)
		{
			if (!track.IsLocal)
				return true;
			Logger.Warning($"Excluding track with uri {track.Uri} and name {track.Name} because it is local, and the Spotify API can't add local tracks to a playlist");
			return false;
		}

		async Task IPlaylistTrackModifier.SendOperations(string playlistId, IEnumerable<ITrackLinkingInfo> currentTracks, IEnumerable<ITrackLinkingInfo> newTracks)
		{
			var newTracksCapped = newTracks.Take(SpotifyConstants.PlaylistSizeLimit).ToArray();
			await PutCorrectTracksInPlaylist(playlistId, currentTracks, newTracksCapped).WithoutContextCapture();
			var jumbledContentsPlaylistVersion = await ExistingPlaylistPlaybackContext.FromSimplePlaylist(SpotifyConfiguration, playlistId);
			await jumbledContentsPlaylistVersion.FullyLoad().WithoutContextCapture();
			var initialSnapshotId = jumbledContentsPlaylistVersion.SpotifyContext.SnapshotId;
			var jumbledUris = jumbledContentsPlaylistVersion.PlaybackOrder.Select(track => track.Uri).ToArray();
			var newUris = newTracksCapped.Select(track => track.Uri).ToArray();
			await ReorderPlaylist(playlistId, initialSnapshotId, jumbledUris, newUris).WithoutContextCapture();
		}

		private async Task PutCorrectTracksInPlaylist(string playlistId, IEnumerable<ITrackLinkingInfo> currentTracks, IEnumerable<ITrackLinkingInfo> newTracksCapped)
		{
			var currentUris = currentTracks.Select(track => track.Uri);
			var newUris = newTracksCapped.Select(track => track.Uri);
			var currentUriCounts = currentUris.ToFrequencyMap();
			var newUriCounts = newUris.ToFrequencyMap();
			var tracksToKeep = currentUris.Where(uri => currentUriCounts.TryGetValue(uri, out var currentCount) && currentCount == 1
														&& newUriCounts.TryGetValue(uri, out var intendedCount) && intendedCount == 1).ToHashSet();
			var tracksToRemove = currentUris.Where(uri => !tracksToKeep.Contains(uri)).Distinct();
			var tracksToAdd = newTracksCapped.Where(ShouldIncludeNonLocalTrack).Select(track => track.Uri).Where(uri => !tracksToKeep.Contains(uri));

			var removeOperations = tracksToKeep.Any() ? RemoveOperation.CreateOperations(tracksToRemove) : new IPlaylistModification[] { new ReplaceOperation() };
			var addOperations = AddOperation.CreateOperations(tracksToAdd);

			await Task.WhenAll(removeOperations.Select(operation => operation.SendRequest(this, playlistId)).ToArray()).WithoutContextCapture();
			await Task.WhenAll(addOperations.Select(operation => operation.SendRequest(this, playlistId)).ToArray()).WithoutContextCapture();
		}

		private async Task ReorderPlaylist(string playlistId, string snapshotId, string[] jumbledOrder, string[] intendedOrder)
		{
			Batch[] GetInitialBatches(string[] jumbledUris)
			{
				var intendedMoves = GetIntendedMoves(jumbledUris, intendedOrder);
				var jumbledBatches = jumbledUris.Select((uri, index) => new Batch(uri, index, intendedMoves[index])).ToArray();
				return jumbledBatches;
			}

			Task<string> QueryCurrentSnapshotId() => (new GetCurrentSnapshotIdOperation() as IPlaylistModification).SendRequest(this, playlistId);


			var playlistSize = jumbledOrder.Length;
			var jumbledBatches = GetInitialBatches(jumbledOrder);

			while (TryGetOperations(jumbledBatches, playlistSize, out var operations, out jumbledBatches))
			{
				await Task.WhenAll(operations.Select(operation => operation.SendRequest(this, playlistId, snapshotId)).ToList()).WithoutContextCapture();
				snapshotId = await QueryCurrentSnapshotId().WithoutContextCapture();
			}			
		}

		private static Dictionary<int, int> GetIntendedMoves(string[] jumbledOrder, string[] intendedOrder)
		{
			var jumbledIndexMap = jumbledOrder.ToIndexMap<string, List<int>>();
			var intendedIndexMap = intendedOrder.ToIndexMap<string, List<int>>();
			return jumbledIndexMap.Keys.SelectMany(uri => jumbledIndexMap[uri].Zip(intendedIndexMap[uri]))
				.ToDictionary(pair => pair.First, pair => pair.Second);
		}

		private bool TryGetOperations(Batch[] jumbledBatches, int playlistSize, out IEnumerable<IPlaylistModification> operations, out Batch[] reorderedBatches)
		{
			if (jumbledBatches.All(batch => batch.RangeStart == batch.TargetIndex)) 
			{
				operations = Array.Empty<IPlaylistModification>();
				reorderedBatches = jumbledBatches;
				return false;
			}

			var paddedBatches = jumbledBatches.Prepend(new BoundaryBatch(-1)).Append(new BoundaryBatch(playlistSize)).ToArray();
			var condensedBatches = CondenseBatches(paddedBatches).ToArray();
			var intendedOrder = condensedBatches.OrderBy(Batch.TargetIndexComparer).ToArray();
			var LCSIndices = LCS.GetLCSIndices(condensedBatches, intendedOrder, Batch.TargetIndexComparer);
			var intendedOrderLCSIndices = LCSIndices.Select(pair => pair.sequence2Index).ToArray();
			var moves = GetIndicesToMove(intendedOrderLCSIndices).Select(move => (intendedOrder[move.indexToMove], intendedOrder[move.indexToInsertBefore])).ToList();
			operations = moves.Select(move => new ReorderOperation(move.Item1.RangeStart, move.Item1.Length, move.Item2.RangeStart)).ToArray();
			var newPrecedingBatches = moves.ToDictionary(move => move.Item2, move => move.Item1);
			var movedBatches = moves.Select(move => move.Item1).ToHashSet();
			reorderedBatches = SimulateReordering(condensedBatches, movedBatches, newPrecedingBatches, playlistSize);
			return operations.Any();
		}

		private Batch[] SimulateReordering(Batch[] originalBatches, ISet<Batch> movedBatches, IReadOnlyDictionary<Batch, Batch> newPrecedingBatches, int playlistTotalSize)
		{
			IEnumerable<Batch> GetNewBatchOrder()
			{
				foreach(var batch in originalBatches.Except(movedBatches))
				{
					if (newPrecedingBatches.TryGetValue(batch, out var precedingBatch))
						yield return precedingBatch;
					yield return batch;
				}
			}
			var reorderedBatches = GetNewBatchOrder().Where(batch => batch.RangeStart >= 0 && batch.RangeStart < playlistTotalSize).ToArray();
			var startIndex = 0;
			for (var i = 0; i < reorderedBatches.Length; i++)
			{
				var newBatch = new Batch(reorderedBatches[i]) { RangeStart = startIndex };
				reorderedBatches[i] = newBatch;
				startIndex += newBatch.Length;
			}
			return reorderedBatches;
		}

		private IEnumerable<(int indexToMove, int indexToInsertBefore)> GetIndicesToMove(int[] lcsIndices)
		{
			int? lowerIndex = null;
			foreach(var upperIndex in lcsIndices)
			{
				if (lowerIndex.HasValue && upperIndex > lowerIndex.Value + 1)
					yield return ((upperIndex + lowerIndex.Value) / 2, upperIndex);
				lowerIndex = upperIndex;
			}
		}

		private IEnumerable<Batch> CondenseBatches(Batch[] initialBatches)
		{
			for (int i = 0; i < initialBatches.Length; i++)
			{
				var batch = initialBatches[i];
				while (i + 1 < initialBatches.Length && batch.CanCombineWith(initialBatches[i + 1]))
					batch += initialBatches[++i];
				yield return batch;
			}
		}

		private class Batch : IComparable<Batch>, IEnumerable<string>
		{
			internal static KeyBasedComparer<Batch, int> TargetIndexComparer { get; } = new KeyBasedComparer<Batch, int>(batch => batch.TargetIndex);

			internal Batch(string uri, int rangeStart, int targetIndex) : this(new[] { uri }, rangeStart, 1, targetIndex)
			{ }

			internal Batch(IEnumerable<string> uris, int rangeStart, int length, int targetIndex)
			{
				RangeStart = rangeStart;
				Length = length;
				Uris = uris;
				TargetIndex = targetIndex;
			}

			internal Batch(Batch batchToCopy)
			{
				RangeStart = batchToCopy.RangeStart;
				Length = batchToCopy.Length;
				Uris = batchToCopy.Uris;
				TargetIndex = batchToCopy.TargetIndex;
			}

			public bool CanCombineWith(Batch otherBatch)
			{
				if (Length == 0 || otherBatch.Length == 0 || Length + otherBatch.Length > SpotifyConstants.PlaylistRequestBatchSize)
					return false;
				var lower = RangeStart < otherBatch.RangeStart ? this : otherBatch;
				var upper = RangeStart < otherBatch.RangeStart ? otherBatch : this;
				return lower.RangeStart + lower.Length == upper.RangeStart && lower.TargetIndex + lower.Length == upper.TargetIndex;
			}

			public static Batch operator +(Batch one, Batch two) => Combine(one, two);

			public static Batch Combine(Batch batch1, Batch batch2)
			{
				if (!batch1.CanCombineWith(batch2))
					throw new ArgumentException($"To combine batches, they must be able to be combined: {batch1}, {batch2}");
				var lower = batch1.RangeStart < batch2.RangeStart ? batch1 : batch2;
				var upper = batch1.RangeStart < batch2.RangeStart ? batch2 : batch1;
				return new Batch(lower.Uris.Concat(upper.Uris), lower.RangeStart, lower.Length + upper.Length, lower.TargetIndex);
			}

			internal int RangeStart { get; set; }
			internal int Length { get; }
			internal int TargetIndex { get; }
			internal IEnumerable<string> Uris { get; }

			public override bool Equals(object obj)
			{
				return obj is Batch batch 
					   && RangeStart == batch.RangeStart
					   && Length == batch.Length
					   && TargetIndex == batch.TargetIndex;
			}

			public override int GetHashCode()
			{
				return HashCode.Combine(RangeStart, Length, TargetIndex);
			}

			public override string ToString()
			{
				return $"[{RangeStart}, {RangeStart + Length}) -> [{TargetIndex}, {TargetIndex + Length})";
			}

			public int CompareTo([AllowNull] Batch other)
			{
				return _comparer.Compare(this, other);
			}

			public IEnumerator<string> GetEnumerator()
			{
				return Uris.GetEnumerator();
			}

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return ((System.Collections.IEnumerable)Uris).GetEnumerator();
			}

			private readonly static IComparer<Batch> _comparer = ComparerUtils.ComparingBy<Batch>(batch => batch.RangeStart).ThenBy(batch => batch.Length);
		}

		private class BoundaryBatch : Batch
		{
			internal BoundaryBatch(int boundary) : base(Array.Empty<string>(), boundary, 0, boundary)
			{ }
		}
	}
}
