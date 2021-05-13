using SpotifyProject.Utils;
using System;
using System.Collections.Generic;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using SpotifyProject.SpotifyPlaybackModifier.Transformations;
using System.Linq;
using System.Runtime.InteropServices;

namespace SpotifyProject.SpotifyPlaybackModifier.TrackLinking
{
	public class LukesTrackLinker<ContextT, TrackT> : ISimpleTrackLinker<ContextT, TrackT, int>
		where ContextT : ISpotifyPlaybackContext<TrackT>
	{
		IEnumerable<IGrouping<int, ITrackLinkingInfo<TrackT>>> IMetadataBasedTrackLinker<ContextT, TrackT, int>.GroupTracksIntoWorks(IEnumerable<ITrackLinkingInfo<TrackT>> trackMetadata)
		{
			var trackMetadataArr = trackMetadata as ITrackLinkingInfo<TrackT>[] ?? trackMetadata.ToArray();
			var infoInputArr = trackMetadataArr.Select(metadata => new TrackLinkingInfoInput(metadata)).ToArray();
			var labels = new int[trackMetadataArr.Length];
			NativeMethods.GroupTracks(infoInputArr, labels, infoInputArr.Length);
			return labels.Zip(trackMetadataArr)
				.GroupBy(pair => pair.First, pair => pair.Second);
		}

		ITrackGrouping<int, TrackT> IMetadataBasedTrackLinker<ContextT, TrackT, int>.DesignateTracksToWork(int work, IEnumerable<TrackT> tracksInWork)
			=> new DumbWork<TrackT>(work, tracksInWork);
	}

	internal static class NativeMethods
	{
		[DllImport("libworkIdentifier.dylib", EntryPoint = "groupTracks", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		internal static extern void GroupTracks(
			[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
			TrackLinkingInfoInput[] trackNames,
			[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
			int[] labels, 
			int numTracks);
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct TrackLinkingInfoInput
	{ 
		private const int MaxArtists = 10;
		public TrackLinkingInfoInput(ITrackLinkingInfo trackInfo)
		{
			UniqueUri = trackInfo.Uri;
			Name = trackInfo.Name;
			AlbumName = trackInfo.AlbumName;
			DiscNumber = trackInfo.AlbumIndex.discNumber;
			TrackNumberOnDisc = trackInfo.AlbumIndex.trackNumber;
			DurationMs = trackInfo.DurationMs;
			ArtistNames = new string[MaxArtists];
			ArtistNames.Fill(trackInfo.ArtistNames);
			NumArtists = ArtistNames.Length;
		}

		public string UniqueUri { get; }
		public string Name { get; }
		public string AlbumName { get; }
		public int DiscNumber { get; }
		public int TrackNumberOnDisc { get; }
		public int DurationMs { get; }

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = MaxArtists)]
		public string[] ArtistNames;
		public int NumArtists { get; }
	}
}
