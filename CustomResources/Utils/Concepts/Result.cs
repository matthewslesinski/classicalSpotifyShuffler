using System;
namespace CustomResources.Utils.Concepts
{
	public readonly record struct Result<T>(bool Exists, T ResultValue)
	{
		public bool HasValue => Exists;
		public bool Success => Exists;
		public bool DidFind => Exists;

		public T FoundValue => ResultValue;

		public static implicit operator Result<T>((bool exists, T resultValue) tuple) => new(tuple.exists, tuple.resultValue);
		public static implicit operator (bool exists, T resultValue)(Result<T> lookupResult) => (lookupResult.Exists, lookupResult.ResultValue);

		public static readonly Result<T> NotFound = new(false, default);
		public static Result<T> Failure => NotFound;
	}
}