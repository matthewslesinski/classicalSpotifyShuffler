using System;
namespace CustomResources.Utils.Concepts
{
	public record struct LookupResult<T>(bool DidFind, T FoundValue)
	{
		public static implicit operator LookupResult<T>((bool didFind, T foundValue) tuple) => new(tuple.didFind, tuple.foundValue);
		public static implicit operator (bool didFind, T foundValue)(LookupResult<T> lookupResult) => (lookupResult.DidFind, lookupResult.FoundValue);
		public static implicit operator LookupResult<T>(Tuple<bool, T> tuple) => new(tuple.Item1, tuple.Item2);
		public static implicit operator Tuple<bool, T>(LookupResult<T> lookupResult) => new Tuple<bool, T>(lookupResult.DidFind, lookupResult.FoundValue);
	}
}