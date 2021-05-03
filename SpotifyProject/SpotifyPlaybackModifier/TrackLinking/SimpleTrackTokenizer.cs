using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace SpotifyProject.SpotifyPlaybackModifier.TrackLinking
{
	public class SimpleTrackTokenizer : ITrackNameTokenizer
	{
		private readonly IEnumerable<char> _tokenIncludesOverrides;
		private readonly Regex _regex;
		public SimpleTrackTokenizer(IEnumerable<char> tokenIncludesOverrides)
		{
			_tokenIncludesOverrides = tokenIncludesOverrides;
			_regex = new Regex(BuildRegexString(_tokenIncludesOverrides));
		}

		public (int index, string token)[] Tokenize(string trackName)
		{
			return _regex.Matches(trackName).Where(match => match.Length > 0).Select(match => (match.Index, match.Value)).ToArray();
		}

		private static string BuildRegexString(IEnumerable<char> tokenOverrides)
		{
			var concatenated = string.Join("", tokenOverrides.Select(c => c.ToString()));
			return $"\"[^\"]*\"|[{concatenated}]|[^\\W{concatenated}]+";
		}

	}
}
