﻿using System;
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
			return Enumerable.Range(0, trackMetadataArr.Length)
				.GroupBy(ind => labels[ind], ind => trackMetadataArr[ind]);
		}

		ITrackGrouping<int, TrackT> IMetadataBasedTrackLinker<ContextT, TrackT, int>.DesignateTracksToWork(int work, IEnumerable<TrackT> tracksInWork)
		{
			return new DumbWork<TrackT>(work, tracksInWork);
		}
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
		private const int MAX_ARTISTS = 10;
		public TrackLinkingInfoInput(ITrackLinkingInfo trackInfo)
		{
			UniqueUri = trackInfo.Uri;
			Name = trackInfo.Name;
			AlbumName = trackInfo.AlbumName;
			DiscNumber = trackInfo.AlbumIndex.discNumber;
			TrackNumberOnDisc = trackInfo.AlbumIndex.trackNumber;
			DurationMs = trackInfo.DurationMs;
			ArtistNames = new string[MAX_ARTISTS];
			ArtistNames.Fill(trackInfo.ArtistNames);
			NumArtists = ArtistNames.Length;
		}

		public string UniqueUri { get; }
		public string Name { get; }
		public string AlbumName { get; }
		public int DiscNumber { get; }
		public int TrackNumberOnDisc { get; }
		public int DurationMs { get; }

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_ARTISTS)]
		public string[] ArtistNames;
		public int NumArtists { get; }
	}
	
	internal static class ArrayExtensions
	{
		public static void Fill<T>(this T[] arr, IEnumerable<T> enumerable)
		{
			var i = 0;
			foreach (var item in enumerable)
			{
				if (i == arr.Length)
				{
					throw new ArgumentException($"Enumerable exceeded array length {arr.Length}");
				}
				arr[i++] = item;
			}
		}
	}
}
