using System;
using System.Collections.Generic;
using System.Linq;

namespace SpotifyProject.SpotifyPlaybackModifier.Transformations
{
	public interface ITrackGrouping<out GroupKeyT, out TrackT> : IGrouping<GroupKeyT, TrackT>
	{
	}
}
