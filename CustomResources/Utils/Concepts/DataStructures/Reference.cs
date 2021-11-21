using System;
namespace CustomResources.Utils.Concepts.DataStructures
{
	/// <summary>
	/// Used as a wrapper around another type in the manner of boxing. This can allow value and reference types alike to be treated as a reference type
	/// in data structures.
	/// </summary>
	/// <typeparam name="T">The underlying type</typeparam>
	public class Reference<T> : IWrapper<T>
	{
		private readonly T _value;
		public Reference(T value)
		{
			_value = value;
		}

		public T WrappedObject => _value;
		public T Value => _value;

		public static implicit operator Reference<T>(T value) => new Reference<T>(value);
		public static implicit operator T(Reference<T> reference) => reference.Value;
	}
}
