using System;
using System.Collections.Generic;
using SpotifyAPI.Web;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using SpotifyProject.SpotifyPlaybackModifier.Transformations;
using System.Linq;
using SpotifyProject.Utils;

namespace SpotifyProject.SpotifyPlaybackModifier.TrackLinking
{
	public class NaiveTrackLinker<ContextT, TrackT> : ITrackLinker<ContextT, TrackT, (string workName, string albumName)>
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

		public IEnumerable<ITrackGrouping<(string workName, string albumName), TrackT>> GroupTracksIntoWorks(ContextT originalContext, IEnumerable<TrackT> tracks)
		{
			var trackOrderWithinWorks = GetTrackOrderWithinWorks(originalContext);
			var firstPass = tracks.GroupBy(track =>
			{
				var description = originalContext.GetMetadataForTrack(track);
				var albumName = description.AlbumName;
				var trackName = string.Join(" ", _tokenizer.Tokenize(description.Name.ToLower()).Select(match => match.token));
				var matches = _tokenizer.Tokenize(trackName);

				foreach (var opusSymbol in _opusIndicators)
				{
					var appearances = matches.Where(match => Equals(match.token, opusSymbol)).ToList();
					if (appearances.Count() == 1)
					{
						for (int i = 0; i < matches.Length - 1; i++)
						{
							if (Equals(matches[i].token, opusSymbol) && int.TryParse(matches[i + 1].token, out var movementNumber))
								return (trackName.Substring(0, matches[i + 1].index + matches[i + 1].token.Length), albumName);
						}
					}
				}

				foreach (var divider in _dividers)
				{
					var dividerAppearances = matches.Skip(2).Where(match => Equals(match.token, divider)).ToList();
					if (dividerAppearances.Count() == 1)
						return (trackName.Substring(0, dividerAppearances.Last().index + 1), albumName);
					if (dividerAppearances.Count() > 1)
					{
						for (int i = 2; i < matches.Length - 1; i++)
						{
							if (matches[i].token.Contains(divider)
								&& (int.TryParse(matches[i + 1].token, out var movementNumber)
									|| (Utils.Utils.IsRomanNumeral(matches[i + 1].token, out var romanNumeral) && (movementNumber = romanNumeral.NumericValue) > 0)))
								return (trackName.Substring(0, matches[i].index + 1), albumName);
						}
					}

				}
				return (trackName, albumName);
			}).Select(group => new SimpleWork<TrackT>(group.Key.Item1, group.Key.albumName,
				group.OrderBy(trackOrderWithinWorks)));
			//.ToDictionary(group => group.Key);
			//var secondPass = firstPass.Select(current =>
			//{
			//	var best = current.Key;
			//	foreach (var otherKey in firstPass.Keys.Where(o => Equals(current.Key.albumName, o.albumName)))
			//	{
			//		if (otherKey.name.Length < best.name.Length && best.name.StartsWith(otherKey.name))
			//			best = otherKey;
			//	}
			//	return new SimpleWork<TrackT>(best.name, current.Key.albumName, current.Value.Tracks);
			//}).GroupBy(group => group.Key).Select(group => new SimpleWork<TrackT>(group.Key.name, group.Key.albumName, group.SelectMany(work => work).OrderBy(t => originalContext.GetMetadataForTrack(t).AlbumIndex)));
			return firstPass.ToList();
		}

		private static IComparer<TrackT> GetTrackOrderWithinWorks(ContextT originalContext) {
			return ComparerUtils.ComparingBy<TrackT, (int discNumber, int trackNumber)>(t => originalContext.GetMetadataForTrack(t).AlbumIndex,
				ComparerUtils.ComparingBy<(int discNumber, int trackNumber)>(i => i.discNumber).ThenBy(i => i.trackNumber));
		}
	}
}
