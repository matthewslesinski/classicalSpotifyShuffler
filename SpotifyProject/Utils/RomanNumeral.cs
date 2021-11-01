using System;
using System.Collections.Generic;
using System.Linq;

namespace SpotifyProject.Utils
{
	public class RomanNumeral
	{
		string _representation;
		public int NumericValue { get; }
		public RomanNumeral(string representation)
		{
			_representation = representation;
			int sum = 0;
			for (int i = 0; i < representation.Length; i++)
			{
				var currValue = representation[i];
				sum = (i + 1 < representation.Length && _translations[currValue] < _translations[representation[i + 1]]) ? sum - _translations[currValue] : sum + _translations[currValue];
			}
			NumericValue = sum;
		}

		public override bool Equals(object obj)
		{
			return obj is RomanNumeral o && NumericValue == o.NumericValue;
		}

		public override int GetHashCode()
		{
			return NumericValue;
		}

		public override string ToString()
		{
			return _representation;
		}

		public static implicit operator int(RomanNumeral n) => n.NumericValue;

		public static bool TryParse(string input, out RomanNumeral output)
		{
			output = null;
			var upperCaseInput = input.ToUpper();
			if (!upperCaseInput.All(_translations.ContainsKey))
				return false;
			output = new RomanNumeral(upperCaseInput);
			return true;
		}

		private static readonly Dictionary<char, int> _translations = new Dictionary<char, int>
		{
			{ 'I', 1 },
			{ 'V', 5 },
			{ 'X', 10 },
			{ 'L', 50 },
			{ 'C', 100 },
			{ 'D', 500 },
			{ 'M', 1000 }
		};
	}
}
