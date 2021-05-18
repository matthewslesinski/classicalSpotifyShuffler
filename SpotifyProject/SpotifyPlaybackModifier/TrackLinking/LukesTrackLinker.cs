using SpotifyProject.Utils;
using System;
using System.Collections.Generic;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using SpotifyProject.SpotifyPlaybackModifier.Transformations;
using System.Linq;
using System.Runtime.InteropServices;
using SpotifyProject.Setup;
using System.IO;
using Newtonsoft.Json;

namespace SpotifyProject.SpotifyPlaybackModifier.TrackLinking
{
	public class LukesTrackLinker<ContextT, TrackT> : ISimpleTrackLinker<ContextT, TrackT, int>
		where ContextT : ISpotifyPlaybackContext<TrackT>
	{

		IEnumerable<IGrouping<int, ITrackLinkingInfo<TrackT>>> IMetadataBasedTrackLinker<ContextT, TrackT, int>.GroupTracksIntoWorks(IEnumerable<ITrackLinkingInfo<TrackT>> trackMetadata)
		{
			var trackMetadataArr = trackMetadata as ITrackLinkingInfo<TrackT>[] ?? trackMetadata.ToArray();
			var infoInputArr = trackMetadataArr.Select(metadata => new TrackLinkingInfoInput(metadata)).ToArray();
			var recordTrackInfoLocation = GlobalCommandLine.Store.GetOptionValue<string>(CommandLineOptions.Names.MetadataRecordFile);
			if (recordTrackInfoLocation != default)
				RecordTrackInfo(recordTrackInfoLocation, infoInputArr);
			var labels = new int[trackMetadataArr.Length];
			NativeMethods.GroupTracks(infoInputArr, labels, infoInputArr.Length, LogByLevelWrapper);
			return labels.Zip(trackMetadataArr)
				.GroupBy(pair => pair.First, pair => pair.Second);
		}

		ITrackGrouping<int, TrackT> IMetadataBasedTrackLinker<ContextT, TrackT, int>.DesignateTracksToWork(int work, IEnumerable<TrackT> tracksInWork)
			=> new DumbWork<TrackT>(work, tracksInWork);

		private static void LogByLevelWrapper(LogLevel logLevel, string msg) => Logger.LogLevelMappings[logLevel](msg, Array.Empty<object>());

		private static void RecordTrackInfo(string outputLocation, IEnumerable<TrackLinkingInfoInput> trackInfos) =>
			File.WriteAllLines(outputLocation, trackInfos.Select(trackInfo => JsonConvert.SerializeObject(trackInfo)));
	}

	internal static class NativeMethods
	{
		public delegate void LoggingCallback(LogLevel logLevel, string msg);

		[DllImport("libworkIdentifier.dylib", EntryPoint = "group_tracks", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		internal static extern void GroupTracks(
			[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
			TrackLinkingInfoInput[] trackNames,
			[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
			int[] labels, 
			int numTracks,
			LoggingCallback logCallback);
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct TrackLinkingInfoInput
	{ 
		private const int MaxArtists = 25;
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
			NumArtists = trackInfo.ArtistNames.Count();
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
