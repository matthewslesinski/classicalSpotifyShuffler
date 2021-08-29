using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using System.Linq;
using SpotifyProject.SpotifyPlaybackModifier.Transformations;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using ApplicationResources.Setup;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackSetters;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackModifiers
{
	public abstract class SpotifyPlaybackModifier<InputContextT, OutputContextT> : SpotifyAccessorBase, IPlaybackModifier<InputContextT>
		where InputContextT : ISpotifyPlaybackContext
		where OutputContextT : ISpotifyPlaybackContext
	{
		public SpotifyPlaybackModifier(SpotifyConfiguration spotifyConfiguration, IPlaybackTransformation<InputContextT, OutputContextT> transformation,
			IPlaybackSetter<OutputContextT, PlaybackStateArgs> playbackSetter)
			: base(spotifyConfiguration)
		{
			_transformer = transformation;
			_playbackSetter = playbackSetter;
		}

		public abstract Task Run(InputContextT inputContext);

		protected readonly IPlaybackTransformation<InputContextT, OutputContextT> _transformer;

		protected readonly IPlaybackSetter<OutputContextT, PlaybackStateArgs> _playbackSetter;
	}
}
