using System;
using System.Collections.Generic;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using SpotifyProject.SpotifyPlaybackModifier.Transformations;
using System.Linq;
using System.Runtime.InteropServices;

namespace SpotifyProject.SpotifyPlaybackModifier.TrackLinking
{
	public class LukesTrackLinker<ContextT, TrackT> : ISimpleTrackLinkerByWorkName<ContextT, TrackT>
		where ContextT : ISpotifyPlaybackContext<TrackT>
	{
		string ISimpleTrackLinkerByWorkName<ContextT, TrackT>.GetWorkNameForTrack(ITrackLinkingInfo trackInfo)
		{
			TrackLinkingInfoInput input = new TrackLinkingInfoInput(trackInfo);
			return NativeMethods.GetWorkFromCPlusPlus(input);
		}
	}

	internal static class NativeMethods
	{
		[DllImport("<LukesDll.dylib>", EntryPoint = "LukesMethod", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		internal static extern string GetWorkFromCPlusPlus(TrackLinkingInfoInput trackNames);
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct TrackLinkingInfoInput
	{
		public TrackLinkingInfoInput(ITrackLinkingInfo trackInfo)
		{
			UniqueUri = trackInfo.Uri;
			Name = trackInfo.Name;
			AlbumName = trackInfo.AlbumName;
			DiscNumber = trackInfo.AlbumIndex.discNumber;
			TrackNumberOnDisc = trackInfo.AlbumIndex.trackNumber;
			DurationMs = trackInfo.DurationMs;
			ArtistNames = trackInfo.ArtistNames.ToArray();
			NumArtists = ArtistNames.Length;
		}

		public string UniqueUri { get; }
		public string Name { get; }
		public string AlbumName { get; }
		public int DiscNumber { get; }
		public int TrackNumberOnDisc { get; }
		public int DurationMs { get; }
		public string[] ArtistNames { get; }
		public int NumArtists { get; }
	}
}
