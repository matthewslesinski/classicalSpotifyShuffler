using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackModifiers
{
	public interface IPlaybackModifier<in InputContextT> : ISpotifyAccessor
		where InputContextT : ISpotifyPlaybackContext
	{
		Task Run(InputContextT inputContext);
	}
}
