using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackSetters;
using SpotifyProject.SpotifyPlaybackModifier.Transformations;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackModifiers
{
	public interface IPlaybackModifier<in InputContextT> : ISpotifyAccessor
		where InputContextT : ISpotifyPlaybackContext
	{
		Task Run(InputContextT inputContext);
	}
}
