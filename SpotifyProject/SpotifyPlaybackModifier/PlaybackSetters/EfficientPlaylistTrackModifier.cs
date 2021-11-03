﻿using System;
using System.Collections;
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
using ApplicationResources.ApplicationUtils.Parameters;
using SpotifyProject.Configuration;
using CustomResources.Utils.GeneralUtils;

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
			// Make sure the playlist contains the right set of tracks
			await PutCorrectTracksInPlaylist(playlistId, currentTracks, newTracksCapped).WithoutContextCapture();
			// Load the playlist to see what needs to be reordered
			var jumbledContentsPlaylistVersion = await ExistingPlaylistPlaybackContext.FromSimplePlaylist(SpotifyConfiguration, playlistId);
			await jumbledContentsPlaylistVersion.FullyLoad().WithoutContextCapture();
			var initialSnapshotId = jumbledContentsPlaylistVersion.SpotifyContext.SnapshotId;
			var jumbledUris = jumbledContentsPlaylistVersion.PlaybackOrder.Select(track => track.Uri).ToArray();
			var newUris = newTracksCapped.Select(track => track.Uri).ToArray();
			// Reorder the tracks in the playlist
			await ReorderPlaylist(playlistId, initialSnapshotId, jumbledUris, newUris).WithoutContextCapture();
		}

		private async Task PutCorrectTracksInPlaylist(string playlistId, IEnumerable<ITrackLinkingInfo> currentTracks, IEnumerable<ITrackLinkingInfo> newTracksCapped)
		{
			var currentUris = currentTracks.Select(track => track.Uri);
			var newUris = newTracksCapped.Select(track => track.Uri);
			var currentUriCounts = currentUris.ToFrequencyMap();
			var newUriCounts = newUris.ToFrequencyMap();
			// Keep tracks that appear once in both the old track list and the new track list
			var tracksToKeep = currentUris.Where(uri => currentUriCounts.TryGetValue(uri, out var currentCount) && currentCount == 1
														&& newUriCounts.TryGetValue(uri, out var intendedCount) && intendedCount == 1).ToHashSet();
			// Remove all other tracks currently in the playlist
			var tracksToRemove = currentUris.Where(tracksToKeep.NotContains).Distinct();
			// Add all tracks not currently in the playlist and being kept. Also exclude local tracks because they can't be added to playlists
			var tracksToAdd = newTracksCapped.Where(ShouldIncludeNonLocalTrack).Select(track => track.Uri).Where(tracksToKeep.NotContains);

			// If no tracks are being kept, we can just clear the playlist
			var removeOperations = tracksToKeep.Any() ? RemoveOperation.CreateOperations(tracksToRemove) : new IPlaylistModification[] { new ReplaceOperation() };
			var addOperations = AddOperation.CreateOperations(tracksToAdd);

			async Task<bool> SendRequest(IPlaylistModification operation) => (await operation.TrySendRequest(this, playlistId).WithoutContextCapture()).ranSuccessfuly;

			// First remove tracks, then add them
			var uncompletedRemoveOperations = removeOperations;
			while (uncompletedRemoveOperations.Any())
				uncompletedRemoveOperations = (await RunAllOperations(uncompletedRemoveOperations, SendRequest).WithoutContextCapture())
					.TryGetValues(false, out var unsuccessfulOperations)
						? unsuccessfulOperations
						: Array.Empty<IPlaylistModification>();

			var uncompletedAddOperations = addOperations;
			while (uncompletedAddOperations.Any())
				uncompletedAddOperations = (await RunAllOperations(uncompletedAddOperations, SendRequest).WithoutContextCapture())
					.TryGetValues(false, out var unsuccessfulOperations)
						? unsuccessfulOperations
						: Array.Empty<IPlaylistModification>();
		}

		private async Task ReorderPlaylist(string playlistId, string snapshotId, string[] jumbledOrder, string[] intendedOrder)
		{
			// Creates the initial set of batches, which have not been combined yet, so they each just contain one track
			Batch[] GetInitialBatches(string[] jumbledUris)
			{
				var intendedMoves = GetIntendedMoves(jumbledUris, intendedOrder);
				var jumbledBatches = jumbledUris.Select((uri, index) => new Batch(uri, index, intendedMoves[index])).ToArray();
				return jumbledBatches;
			}

			Task<string> QueryCurrentSnapshotId() => (new GetCurrentSnapshotIdOperation() as IPlaylistModification).SendRequest(this, playlistId);


			var playlistSize = jumbledOrder.Length;
			if (playlistSize != intendedOrder.Length)
				throw new ArgumentException($"The current version of the playlist has {playlistSize} tracks, but is supposed to have {intendedOrder.Length} tracks");

			// Put each track into its own batch, which includes the intended index for it in the intended order
			var jumbledBatches = GetInitialBatches(jumbledOrder);

			// Gets the operations that can all be done without impacting each other. OperationsAndBatches contains pairs of operations with the batch the operation moves.
			// PaddedBatches is just jumbledBatches with padding, but it's necessary for calling SimulateReordering below. NewlyPrecedingBatches is a map from batch to the batch
			// That gets moved before it in the outputted operations.
			while (TryGetOperations(jumbledBatches, playlistSize, out var operationsAndBatches, out var paddedBatches, out var newlyPrecedingBatches))
			{
				// Run each operation, and if it's successful, add its batch to the movedBatches set
				var separatedResults = await RunAllOperations(operationsAndBatches,
					async pair => (await pair.operation.TrySendRequest(this, playlistId, snapshotId).WithoutContextCapture()).ranSuccessfuly);

				var movedBatches = separatedResults.TryGetValues(true, out var successfulOperations)
					? successfulOperations.Select(pair => pair.movedBatch).ToHashSet()
					: Exceptions.Throw<ISet<Batch>>($"The Spotify API failed to move any of the batches when reordering the playlist with id {playlistId} and snapshotId {snapshotId}");

				// Simulate the reordering with all of the batches that are successfully moved
				jumbledBatches = SimulateReordering(paddedBatches, movedBatches, newlyPrecedingBatches);

				snapshotId = await QueryCurrentSnapshotId().WithoutContextCapture();
			}			
		}

		private async Task<ILookup<bool, OperationT>> RunAllOperations<OperationT>(
			IEnumerable<OperationT> operations, Func<OperationT, Task<bool>> sendAction)
		{
			// Based on the SerializeOperations parameter, run all the operations either sequentially or in parallel
			var inputsWithResults = TaskParameters.Get<bool>(SpotifyParameters.SerializeOperations)
				? await operations.ToAsyncEnumerable().SelectAwait(async operation => (operation, await sendAction(operation).WithoutContextCapture())).ToArrayAsync().WithoutContextCapture()
				: await Task.WhenAll(operations.Select(async operation => (operation, await sendAction(operation).WithoutContextCapture())).ToArray()).WithoutContextCapture();
			return inputsWithResults.ToLookup(inputWithResult => inputWithResult.Item2, inputWithResult => inputWithResult.Item1);
		}

		private static Dictionary<int, int> GetIntendedMoves(string[] jumbledOrder, string[] intendedOrder)
		{
			// get maps from each uri to its index in its ordering
			var jumbledIndexMap = jumbledOrder.ToIndexMap<string, List<int>>();
			var intendedIndexMap = intendedOrder.ToIndexMap<string, List<int>>();
			// Make a dictionary that has each index for the jumbled order associated with the index its uri ends up in the intended order
			return jumbledIndexMap.Keys.SelectMany(uri => jumbledIndexMap[uri].Zip(intendedIndexMap[uri]))
				.ToDictionary(pair => pair.First, pair => pair.Second);
		}

		private static bool TryGetOperations(Batch[] jumbledBatches, int playlistSize, out IEnumerable<(IPlaylistModification operation, Batch movedBatch)> operations,
			out Batch[] condensedBatches, out IReadOnlyDictionary<Batch, Batch> newlyPrecedingBatches)
		{
			// Check if we're already done
			if (jumbledBatches.All(batch => batch.RangeStart == batch.TargetIndex)) 
			{
				newlyPrecedingBatches = new Dictionary<Batch, Batch>();
				condensedBatches = Array.Empty<Batch>();
				operations = Array.Empty<(IPlaylistModification operation, Batch movedBatch)>();
				return false;
			}

			// Add a boundary batch on each end of the order
			var paddedBatches = jumbledBatches.Prepend(new BoundaryBatch(0)).Append(new BoundaryBatch(playlistSize)).ToArray();
			// Combine batches that are adjacent and should remain adjacent
			condensedBatches = CondenseBatches(paddedBatches).ToArray();
			// Order the batches by where they'll end up
			var intendedOrder = condensedBatches.OrderBy(Batch.TargetIndexComparer).ToArray();
			// Gets a sequence of pairs of indices corresponding to the longest common subsequence (i.e. the batches that don't need to be moved).
			// Each pair is the index in the jumbled batches and the index in the intended order
			var LCSIndices = LCS.GetLCSIndices(condensedBatches, intendedOrder, Batch.TargetIndexComparer);
			var intendedOrderLCSIndices = LCSIndices.Select(pair => pair.sequence2Index).ToArray();
			// Get pairs of indices that refer to batches in the intended order where the first needs to be moved and should end up right before the second,
			// which is part of the LCS (and should stay put). Each one that should be moved is halfway (rounded down) in between LCS elements in the intended order
			var moves = GetIndicesToMove(intendedOrderLCSIndices)
				// Turn those indices into the batches they refer to
				.Select(move => (intendedOrder[move.indexToMove], intendedOrder[move.indexToInsertBefore])).ToList();
			// Create the operations to send to spotify to move those batches
			operations = moves.Select<(Batch batchToMove, Batch batchToInsertBefore), (IPlaylistModification operation, Batch batchToMove)>(
				move => (new ReorderOperation(move.batchToMove.RangeStart, move.batchToMove.Length, move.batchToInsertBefore.RangeStart),
				move.batchToMove)).ToArray();

			newlyPrecedingBatches = moves.ToDictionary(move => move.Item2, move => move.Item1);
			return operations.Any();
		}

		private static Batch[] SimulateReordering(Batch[] originalBatches, ISet<Batch> movedBatches, IReadOnlyDictionary<Batch, Batch> newlyPrecedingBatches)
		{
			// Enumerates the batches in the order they should end up in
			IEnumerable<Batch> GetNewBatchOrder()
			{
				foreach(var batch in originalBatches.Except(movedBatches))
				{
					if (newlyPrecedingBatches.TryGetValue(batch, out var precedingBatch) && movedBatches.Contains(precedingBatch))
						yield return precedingBatch;
					yield return batch;
				}
			}
			// Also remove the padding Batches
			var reorderedBatches = GetNewBatchOrder().Where(batch => batch.Length > 0).ToArray();
			var startIndex = 0;
			// reset each batch's RangeStart to be based on its new position
			for (var i = 0; i < reorderedBatches.Length; i++)
			{
				var newBatch = new Batch(reorderedBatches[i]) { RangeStart = startIndex };
				reorderedBatches[i] = newBatch;
				startIndex += newBatch.Length;
			}
			return reorderedBatches;
		}

		private static IEnumerable<(int indexToMove, int indexToInsertBefore)> GetIndicesToMove(int[] lcsIndices)
		{
			// enumerate through each index and next index in lcsIndices
			int? lowerIndex = null;
			foreach(var upperIndex in lcsIndices)
			{
				// if the batches should have something go in between them, find the batch halfway in between and plan to move it to right after the lowerIndex batch
				if (lowerIndex.HasValue && upperIndex > lowerIndex.Value + 1)
					yield return ((upperIndex + lowerIndex.Value) / 2, upperIndex);
				lowerIndex = upperIndex;
			}
		}

		private static IEnumerable<Batch> CondenseBatches(Batch[] initialBatches)
		{
			for (int i = 0; i < initialBatches.Length; i++)
			{
				var batch = initialBatches[i];
				// While subsequent batches can be combined
				while (i + 1 < initialBatches.Length && batch.CanCombineWith(initialBatches[i + 1]))
				{
					// combine the batches
					var combinedBatches = batch + initialBatches[++i];
					// yield any full batches
					foreach (var extraBatch in combinedBatches.GetRange(0, combinedBatches.Count - 1))
						yield return extraBatch;
					// continue with the unfilled batch
					batch = combinedBatches[^1];
				}
				// The batch cannot be combined, so yield it
				yield return batch;
			}
		}

		/*
		 * Represents a group of tracks in the original track order that remain as a group in the intended order and therefore can be moved with one reorder operation
		 */
		private class Batch : IComparable<Batch>, IEnumerable<string>
		{
			internal static KeyBasedComparer<Batch, (int target, int length)> TargetIndexComparer { get; } = new KeyBasedComparer<Batch, (int target, int length)>(
				batch => (batch.TargetIndex, batch.Length),
				ComparerUtils.ComparingBy<(int target, int length)>(batch => batch.target).ThenBy(batch => batch.length));

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
				if (Length == 0 || otherBatch.Length == 0)
					return false;
				var lower = RangeStart < otherBatch.RangeStart ? this : otherBatch;
				var upper = RangeStart < otherBatch.RangeStart ? otherBatch : this;
				if (lower.Length == TaskParameters.Get<int>(SpotifyParameters.PlaylistRequestBatchSize))
					return false;
				return lower.RangeStart + lower.Length == upper.RangeStart && lower.TargetIndex + lower.Length == upper.TargetIndex;
			}

			public static List<Batch> operator +(Batch one, Batch two) => Combine(one, two);

			public static List<Batch> Combine(Batch batch1, Batch batch2)
			{
				if (!batch1.CanCombineWith(batch2))
					throw new ArgumentException($"To combine batches, they must be able to be combined: {batch1}, {batch2}");
				var lower = batch1.RangeStart < batch2.RangeStart ? batch1 : batch2;
				var upper = batch1.RangeStart < batch2.RangeStart ? batch2 : batch1;
				var sizeLimit = TaskParameters.Get<int>(SpotifyParameters.PlaylistRequestBatchSize);
				if (batch1.Length + batch2.Length > sizeLimit)
				{
					var allUris = lower.Uris.Concat(upper.Uris).ToList();
					return new List<Batch>
					{
						new Batch(allUris.GetRange(0, sizeLimit), lower.RangeStart, sizeLimit, lower.TargetIndex),
						new Batch(allUris.GetRange(sizeLimit, allUris.Count - sizeLimit), lower.RangeStart + sizeLimit, allUris.Count - sizeLimit, lower.TargetIndex + sizeLimit)
					};
				}
				else
					return new List<Batch> { new Batch(lower.Uris.Concat(upper.Uris), lower.RangeStart, lower.Length + upper.Length, lower.TargetIndex) };
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

			IEnumerator IEnumerable.GetEnumerator() => Uris.GetEnumerator();
			public IEnumerator<string> GetEnumerator()
			{
				return Uris.GetEnumerator();
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
