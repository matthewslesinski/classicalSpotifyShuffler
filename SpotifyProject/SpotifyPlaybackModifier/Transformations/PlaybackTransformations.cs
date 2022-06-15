using System;
using SpotifyAPI.Web;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;
using System.Linq;
using System.Collections.Generic;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.Concepts;

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
					ReorderedAllLikedTracksPlaybackContext<IOriginalAllLikedTracksPlaybackContext>.FromContextAndTracks(initialContext, tracks)) },
				{ PlaybackContextType.CustomQueue, new PlaybackTransformations<ICustomPlaybackContext, IPlayableTrackLinkingInfo>((initialContext, tracks) =>
					ReorderedCustomPlaybackContext<ICustomPlaybackContext>.FromContextAndTracks(initialContext, tracks)) }
			};


        public static bool TryGetTransformation<ContextT, TrackT>(PlaybackContextType contextType, out IPlaybackTransformationsStore<ContextT, TrackT> transformations)
            where ContextT : ISpotifyPlaybackContext<TrackT> => 
				_transformations.TryGetCastedValue(contextType, out transformations);

    }

	public interface IPlaybackTransformationsStore<ContextT, TrackT> where ContextT : ISpotifyPlaybackContext<TrackT>
	{
		
		IPlaybackTransformation<ContextT,
			IReorderedPlaybackContext<TrackT, ContextT>> SameOrder { get; }

		IPlaybackTransformation<ContextT,
			IReorderedPlaybackContext<TrackT, ContextT>> ReverseOrder { get; }

		IPlaybackTransformation<ContextT,
			IReorderedPlaybackContext<TrackT, ContextT>> SimpleShuffle { get; }
		
		IPlaybackTransformation<ContextT,
			IReorderedPlaybackContext<TrackT, ContextT>> ReorderingByTrackName { get; }

		IPlaybackTransformation<ContextT,
			IReorderedPlaybackContext<TrackT, ContextT>> ReorderingByTrackUri { get; }

		IPlaybackTransformation<ContextT,
			IReorderedPlaybackContext<TrackT, ContextT>> SimpleShuffleByWork { get; }

		IPlaybackTransformation<ContextT,
			IReorderedPlaybackContext<TrackT, ContextT>> LukesShuffle { get; }
	}

	public class PlaybackTransformations<ContextT, TrackT> : IPlaybackTransformationsStore<ContextT, TrackT> where ContextT : ISpotifyPlaybackContext<TrackT>
	{
		public PlaybackTransformations(Func<ContextT, IEnumerable<TrackT>,
			IReorderedPlaybackContext<TrackT, ContextT>> contextConstructor)
		{
			SameOrder = new SameOrdering<ContextT,
				IReorderedPlaybackContext<TrackT, ContextT>, TrackT>(contextConstructor);

			ReverseOrder = new ReverseOrdering<ContextT,
				IReorderedPlaybackContext<TrackT, ContextT>, TrackT>(contextConstructor);

			SimpleShuffle = new SimpleReordering<ContextT,
				IReorderedPlaybackContext<TrackT, ContextT>, TrackT>(contextConstructor);

			ReorderingByTrackName = new SortedReordering<ContextT,
				IReorderedPlaybackContext<TrackT, ContextT>, TrackT>(contextConstructor, ComparerUtils.ComparingBy<ITrackLinkingInfo>(track => track.Name));

			ReorderingByTrackUri = new SortedReordering<ContextT,
				IReorderedPlaybackContext<TrackT, ContextT>, TrackT>(contextConstructor, ComparerUtils.ComparingBy<ITrackLinkingInfo>(track => track.Uri));

			SimpleShuffleByWork = new SimpleWorkShuffle<ContextT,
				IReorderedPlaybackContext<TrackT, ContextT>, TrackT, (string workName, string albumName, string albumUri)>(
				contextConstructor, new NaiveTrackLinker<ContextT, TrackT>(new[] { "op", "k", "bwv", "woo", "d", "bb", "hwv", "s", "sz", "l" }, new[] { "/", ":", "-" }));

			LukesShuffle = new SimpleWorkShuffle<ContextT,
				IReorderedPlaybackContext<TrackT, ContextT>, TrackT, int>(
				contextConstructor, new LukesTrackLinker<ContextT, TrackT>());
		}

		
		public IPlaybackTransformation<ContextT,
			IReorderedPlaybackContext<TrackT, ContextT>> SameOrder { get; }

		public IPlaybackTransformation<ContextT,
			IReorderedPlaybackContext<TrackT, ContextT>> ReverseOrder { get; }

		public IPlaybackTransformation<ContextT,
			IReorderedPlaybackContext<TrackT, ContextT>> SimpleShuffle { get; }

		public IPlaybackTransformation<ContextT,
			IReorderedPlaybackContext<TrackT, ContextT>> ReorderingByTrackName { get; }

		public IPlaybackTransformation<ContextT,
			IReorderedPlaybackContext<TrackT, ContextT>> ReorderingByTrackUri { get; }

		public IPlaybackTransformation<ContextT,
			IReorderedPlaybackContext<TrackT, ContextT>> SimpleShuffleByWork { get; }

		public IPlaybackTransformation<ContextT,
			IReorderedPlaybackContext<TrackT, ContextT>> LukesShuffle { get; }

	}

}
