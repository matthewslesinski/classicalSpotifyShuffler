using System;
using System.Threading;
using System.Threading.Tasks;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts
{
	public interface IOriginalPlaybackContext : ISpotifyPlaybackContext
	{
		Task FullyLoad(CancellationToken cancellationToken = default);
	}
}
