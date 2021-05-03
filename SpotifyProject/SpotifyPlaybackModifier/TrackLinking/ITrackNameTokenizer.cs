using System;
using System.Collections.Generic;

namespace SpotifyProject.SpotifyPlaybackModifier.TrackLinking
{
	public interface ITrackNameTokenizer
	{
		(int index, string token)[] Tokenize(string trackName);
	}
}
