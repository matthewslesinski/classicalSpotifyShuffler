using System;
using System.Threading.Tasks;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackSetters
{
	public interface IPlaybackSetter<in ContextT, in ArgsT> : ISpotifyAccessor where ContextT : ISpotifyPlaybackContext where ArgsT : IPlaybackSetterArgs
	{
		Task SetPlayback(ContextT context, ArgsT args);
	}

	public interface IPlaybackSetterArgs { }
}
