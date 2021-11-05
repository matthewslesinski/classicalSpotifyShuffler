using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;
using System.Linq;
using SpotifyProject.Utils;
using CustomResources.Utils.Extensions;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackSetters
{
	public class BasicPlaylistTrackModifier : SpotifyAccessorBase, IPlaylistTrackModifier
	{
		public BasicPlaylistTrackModifier(SpotifyConfiguration spotifyConfiguration) : base(spotifyConfiguration)
		{ }

		async Task IPlaylistTrackModifier.SendOperations(string playlistId, IEnumerable<ITrackLinkingInfo> currentTracks, IEnumerable<ITrackLinkingInfo> newTracks)
		{
			var operations = GetOperations(newTracks);
			string snapshotId = null;
			foreach(var operation in operations)
			{
				snapshotId = await operation.SendRequest(this, playlistId, snapshotId).WithoutContextCapture();
			}
		}

		private static IEnumerable<IPlaylistModification> GetOperations(IEnumerable<ITrackLinkingInfo> newTracks)
		{
			var removeOperation = new ReplaceOperation();
			var urisToAdd = newTracks.Where(track => !track.IsLocal).Take(SpotifyConstants.PlaylistSizeLimit);
			return new IPlaylistModification[] { removeOperation }
				.Concat(AddOperation.CreateOperations(urisToAdd))
				.Select(operation => operation);
		}
	}
}
