using System;
using System.Collections.Generic;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using System.Linq;
using Util = SpotifyProject.Utils.GeneralUtils.Utils;

namespace SpotifyProject.SpotifyPlaybackModifier.TrackLinking
{
	public class NaiveTrackLinker<ContextT, TrackT> : ISimpleTrackLinkerByWorkName<ContextT, TrackT>
		where ContextT : ISpotifyPlaybackContext<TrackT>
	{
		private readonly IEnumerable<string> _dividers;
		private readonly IEnumerable<string> _opusIndicators;
		private readonly ITrackNameTokenizer _tokenizer;

		public NaiveTrackLinker(string[] opusIndicators, string[] dividers = null, ITrackNameTokenizer tokenizer = null)
		{
			_dividers = dividers == null ? Array.Empty<string>() : dividers;
			_opusIndicators = opusIndicators;
			_tokenizer = tokenizer ?? new SimpleTrackTokenizer(dividers.Select(divider => Convert.ToChar(divider)));
		}

		string ISimpleTrackLinkerByWorkName<ContextT, TrackT>.GetWorkNameForTrack(ITrackLinkingInfo trackInfo)
		{
			var trackName = string.Join(" ", _tokenizer.Tokenize(trackInfo.Name.ToLower()).Select(match => match.token));
			var matches = _tokenizer.Tokenize(trackName);

			foreach (var opusSymbol in _opusIndicators)
			{
				var appearances = matches.Where(match => Equals(match.token, opusSymbol)).ToList();
				if (appearances.Count() == 1)
				{
					for (int i = 0; i < matches.Length - 1; i++)
					{
						if (Equals(matches[i].token, opusSymbol) && int.TryParse(matches[i + 1].token, out var movementNumber))
							return trackName.Substring(0, matches[i + 1].index + matches[i + 1].token.Length);
					}
				}
			}

			foreach (var divider in _dividers)
			{
				var dividerAppearances = matches.Skip(2).Where(match => Equals(match.token, divider)).ToList();
				if (dividerAppearances.Count == 1)
					return trackName.Substring(0, dividerAppearances.Last().index + 1);
				if (dividerAppearances.Count > 1)
				{
					for (int i = 2; i < matches.Length - 1; i++)
					{
						if (matches[i].token.Contains(divider)
							&& (int.TryParse(matches[i + 1].token, out var movementNumber)
								|| (Util.IsRomanNumeral(matches[i + 1].token, out var romanNumeral) && (movementNumber = romanNumeral.NumericValue) > 0)))
							return trackName.Substring(0, matches[i].index + 1);
					}
				}

			}
			return trackName;
		}
	}
}
