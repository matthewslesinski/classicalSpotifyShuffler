using System;
using System.Threading;

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

		public override bool Equals(object obj) => obj is Reference<T> otherRef && Equals(_value, otherRef._value);

		public override int GetHashCode() => _value.GetHashCode();

		public override string ToString() => $"<{_value}>";

		public static implicit operator Reference<T>(T value) => new Reference<T>(value);
		public static implicit operator T(Reference<T> reference) => reference.Value;
	}

	public class MutableReference<T> : IWrapper<T>
	{
		private Reference<T> _value;
		public MutableReference(T value)
		{
			_value = value;
		}

		public T WrappedObject => _value;
		public T Value { get => _value; set => Change(value); }

		public void Change(T newValue) => _value = newValue;
		public T AtomicExchange(T newValue) => Interlocked.Exchange(ref _value, newValue);
		public T AtomicCompareExchange(T newValue, T comparand) => Interlocked.CompareExchange(ref _value, newValue, comparand);

		public override bool Equals(object obj) => obj is MutableReference<T> otherRef && Equals(_value, otherRef._value);

		public override int GetHashCode() => _value.GetHashCode();

		public override string ToString() => $"<{_value}>";

		public static implicit operator T(MutableReference<T> reference) => reference.Value;
	}
}
