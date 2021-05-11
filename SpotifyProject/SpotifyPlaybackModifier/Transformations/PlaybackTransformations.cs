using System;
using SpotifyAPI.Web;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;
using System.Linq;
using System.Collections.Generic;

namespace SpotifyProject.SpotifyPlaybackModifier.Transformations
{

	public static class PlaybackTransformations
	{
		private static readonly Dictionary<PlaybackContextType, object> _transformations =
			new Dictionary<PlaybackContextType, object>
			{
				{ PlaybackContextType.Album, new PlaybackTransformations<IOriginalAlbumPlaybackContext, SimpleTrack>((initialContext, tracks) =>
					ReorderedAlbumPlaybackContext<IOriginalAlbumPlaybackContext>.FromContextAndTracks(initialContext, tracks)) },
				{ PlaybackContextType.Playlist, new PlaybackTransformations<IOriginalPlaylistPlaybackContext, FullTrack>((initialContext, tracks) =>
					ReorderedPlaylistPlaybackContext<IOriginalPlaylistPlaybackContext>.FromContextAndTracks(initialContext, tracks)) },
				{ PlaybackContextType.Artist, new PlaybackTransformations<IOriginalArtistPlaybackContext, SimpleTrackAndAlbumWrapper>((initialContext, tracks) =>
					ReorderedArtistPlaybackContext<IOriginalArtistPlaybackContext>.FromContextAndTracks(initialContext, tracks)) },
				{ PlaybackContextType.AllLikedTracks, new PlaybackTransformations<IOriginalAllLikedTracksPlaybackContext, FullTrack>((initialContext, tracks) =>
					ReorderedAllLikedTracksPlaybackContext<IOriginalAllLikedTracksPlaybackContext>.FromContextAndTracks(initialContext, tracks)) }
			};


		public static bool TryGetTransformation<ContextT, TrackT>(PlaybackContextType contextType, out IPlaybackTransformationsStore<ContextT, TrackT> transformations)
			where ContextT : ISpotifyPlaybackContext<TrackT>
		{
			transformations = null;
			return _transformations.TryGetValue(contextType, out var transformationObj)
				&& (transformations = transformationObj as IPlaybackTransformationsStore<ContextT, TrackT>) != default;
		}
		
	}

	public interface IPlaybackTransformationsStore<ContextT, TrackT> where ContextT : ISpotifyPlaybackContext<TrackT>
	{

		ITrackReorderingPlaybackTransformation<ContextT,
			IReorderedPlaybackContext<TrackT, ContextT>, TrackT> SameOrder { get; }

		ITrackReorderingPlaybackTransformation<ContextT,
			IReorderedPlaybackContext<TrackT, ContextT>, TrackT> SimpleShuffle { get; }

		ITrackReorderingPlaybackTransformation<ContextT,
			IReorderedPlaybackContext<TrackT, ContextT>, TrackT> SimpleShuffleByWork { get; }

		ITrackReorderingPlaybackTransformation<ContextT,
			IReorderedPlaybackContext<TrackT, ContextT>, TrackT> LukesShuffle { get; }
	}

	public class PlaybackTransformations<ContextT, TrackT> : IPlaybackTransformationsStore<ContextT, TrackT> where ContextT : ISpotifyPlaybackContext<TrackT>
	{
		public PlaybackTransformations(Func<ContextT, IEnumerable<TrackT>,
			IReorderedPlaybackContext<TrackT, ContextT>> contextConstructor)
		{
			SameOrder = new SameOrdering<ContextT,
				IReorderedPlaybackContext<TrackT, ContextT>, TrackT>(contextConstructor);

			SimpleShuffle = new SimpleReordering<ContextT,
				IReorderedPlaybackContext<TrackT, ContextT>, TrackT>(contextConstructor);

			SimpleShuffleByWork =new SimpleWorkShuffle<ContextT,
				IReorderedPlaybackContext<TrackT, ContextT>, TrackT>(
				contextConstructor, new NaiveTrackLinker<ContextT, TrackT>(new[] { "op", "k", "bwv", "woo", "d", "bb", "hwv", "s", "sz", "l" }, new[] { "/", ":", "-" }));

			LukesShuffle = new SimpleWorkShuffle<ContextT,
				IReorderedPlaybackContext<TrackT, ContextT>, TrackT>(
				contextConstructor, new LukesTrackLinker<ContextT, TrackT>());
		}


		public ITrackReorderingPlaybackTransformation<ContextT,
			IReorderedPlaybackContext<TrackT, ContextT>, TrackT> SameOrder { get; }

		public ITrackReorderingPlaybackTransformation<ContextT,
			IReorderedPlaybackContext<TrackT, ContextT>, TrackT> SimpleShuffle { get; }
		
		public ITrackReorderingPlaybackTransformation<ContextT,
			IReorderedPlaybackContext<TrackT, ContextT>, TrackT> SimpleShuffleByWork { get; }

		public ITrackReorderingPlaybackTransformation<ContextT,
			IReorderedPlaybackContext<TrackT, ContextT>, TrackT> LukesShuffle { get; }

	}

}
