using System;
using System.Threading.Tasks;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackSetters
{
	public interface IContextSetter<in ContextT, in ArgsT> : ISpotifyAccessor where ContextT : ISpotifyPlaybackContext where ArgsT : IContextSetterArgs
	{
		Task SetContext(ContextT context, ArgsT args);
	}

	public interface IContextSetterArgs { }
}
