﻿using System;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts
{
	public interface ISpotifyPlaybackContext
	{
		PlaybackContextType ContextType { get; }
		bool TryGetSpotifyId(out string contextId);
		bool TryGetSpotifyUri(out string contextUri);
	}

	public interface ISpotifyPlaybackContext<TrackT> : ISpotifyPlaybackContext, ISpotifyQueue<TrackT>
	{
		IPlayableTrackLinkingInfo<TrackT> GetMetadataForTrack(TrackT track);
	}

	public static class ContextExtensions
	{
		public static bool IsLocal<TrackT>(this ISpotifyPlaybackContext<TrackT> context, TrackT track) =>
			context.GetMetadataForTrack(track).IsLocal;

		public static string GetUriForTrack<TrackT>(this ISpotifyPlaybackContext<TrackT> context, TrackT track) =>
			context.GetMetadataForTrack(track).Uri;
	}

	public enum PlaybackContextType
	{
		Undefined,
		Album,
		Artist,
		Playlist,
		AllLikedTracks,
		CustomQueue
	}
}
