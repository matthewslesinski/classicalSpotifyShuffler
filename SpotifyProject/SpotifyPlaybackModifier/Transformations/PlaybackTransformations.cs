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
				{ PlaybackContextType.Album, new PlaybackTransformations<FullAlbum, SimpleTrack>((initialContext, tracks) =>
					ReorderedAlbumPlaybackContext<IStaticPlaybackContext<FullAlbum, SimpleTrack>>.FromContextAndTracks(initialContext, tracks)) },
				{ PlaybackContextType.Playlist, new PlaybackTransformations<FullPlaylist, FullTrack>((initialContext, tracks) =>
					ReorderedPlaylistPlaybackContext<IStaticPlaybackContext<FullPlaylist, FullTrack>>.FromContextAndTracks(initialContext, tracks)) },
				{ PlaybackContextType.Artist, new PlaybackTransformations<FullArtist, SimpleTrackAndAlbumWrapper>((initialContext, tracks) =>
					ReorderedArtistPlaybackContext<IStaticPlaybackContext<FullArtist, SimpleTrackAndAlbumWrapper>>.FromContextAndTracks(initialContext, tracks)) }
			};


		public static bool TryGetTransformation<SpotifyItemT, TrackT>(PlaybackContextType contextType, out IPlaybackTransformationsStore<SpotifyItemT, TrackT> transformations)
		{
			transformations = null;
			return _transformations.TryGetValue(contextType, out var transformationObj)
				&& (transformations = transformationObj as IPlaybackTransformationsStore<SpotifyItemT, TrackT>) != default;
		}
		
	}

	public interface IPlaybackTransformationsStore<SpotifyItemT, TrackT>
	{

		ITrackReorderingPlaybackTransformation<IStaticPlaybackContext<SpotifyItemT, TrackT>,
			IReorderedPlaybackContext<TrackT, IStaticPlaybackContext<SpotifyItemT, TrackT>>, TrackT> SameOrder { get; }

		ITrackReorderingPlaybackTransformation<IStaticPlaybackContext<SpotifyItemT, TrackT>,
			IReorderedPlaybackContext<TrackT, IStaticPlaybackContext<SpotifyItemT, TrackT>>, TrackT> SimpleShuffle { get; }

		ITrackReorderingPlaybackTransformation<IStaticPlaybackContext<SpotifyItemT, TrackT>,
			IReorderedPlaybackContext<TrackT, IStaticPlaybackContext<SpotifyItemT, TrackT>>, TrackT> SimpleShuffleByWork { get; }
	}

	public class PlaybackTransformations<SpotifyItemT, TrackT> : IPlaybackTransformationsStore<SpotifyItemT, TrackT>
	{
		public PlaybackTransformations(Func<IStaticPlaybackContext<SpotifyItemT, TrackT>, IEnumerable<TrackT>,
			IReorderedPlaybackContext<TrackT, IStaticPlaybackContext<SpotifyItemT, TrackT>>> contextConstructor)
		{
			SameOrder = new SameOrdering<IStaticPlaybackContext<SpotifyItemT, TrackT>,
				IReorderedPlaybackContext<TrackT, IStaticPlaybackContext<SpotifyItemT, TrackT>>, TrackT>(contextConstructor);

			SimpleShuffle = new SimpleReordering<IStaticPlaybackContext<SpotifyItemT, TrackT>,
				IReorderedPlaybackContext<TrackT, IStaticPlaybackContext<SpotifyItemT, TrackT>>, TrackT>(contextConstructor);

			SimpleShuffleByWork =new SimpleWorkShuffle<IStaticPlaybackContext<SpotifyItemT, TrackT>,
				IReorderedPlaybackContext<TrackT, IStaticPlaybackContext<SpotifyItemT, TrackT>>, TrackT>(
				contextConstructor, new NaiveTrackLinker<IStaticPlaybackContext<SpotifyItemT, TrackT>, TrackT>(new[] { "op", "k", "bwv", "woo", "d", "bb", "hwv", "s", "sz", "l" }, new[] { "/", ":", "-" }));
		}


		public ITrackReorderingPlaybackTransformation<IStaticPlaybackContext<SpotifyItemT, TrackT>,
			IReorderedPlaybackContext<TrackT, IStaticPlaybackContext<SpotifyItemT, TrackT>>, TrackT> SameOrder { get; }

		public ITrackReorderingPlaybackTransformation<IStaticPlaybackContext<SpotifyItemT, TrackT>,
			IReorderedPlaybackContext<TrackT, IStaticPlaybackContext<SpotifyItemT, TrackT>>, TrackT> SimpleShuffle { get; }

		public ITrackReorderingPlaybackTransformation<IStaticPlaybackContext<SpotifyItemT, TrackT>,
			IReorderedPlaybackContext<TrackT, IStaticPlaybackContext<SpotifyItemT, TrackT>>, TrackT> SimpleShuffleByWork { get; }

	}

}
