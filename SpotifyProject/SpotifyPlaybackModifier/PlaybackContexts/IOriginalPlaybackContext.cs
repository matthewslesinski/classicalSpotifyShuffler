using System;
using System.Threading.Tasks;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts
{
	public interface IOriginalPlaybackContext : ISpotifyPlaybackContext
	{
		Task FullyLoad();
	}
}
