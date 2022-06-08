using System;
using System.Collections.Generic;
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
		public delegate void OnChangeAction(T oldValue, T newValue);

		private Reference<T> _value;
		public MutableReference(T value)
		{
			_value = value;
		}

		public event OnChangeAction OnChange;

		public T WrappedObject => _value;
		public T Value { get => _value; set => Change(value); }
		public T ValueContainer => _value;

		public T Change(T newValue)
		{
			var oldValue = _value;
			_value = newValue;
			OnChange?.Invoke(oldValue, newValue);
			return oldValue;
		}

		public T AtomicExchange(T newValue)
		{
			var oldValue = Interlocked.Exchange(ref _value, newValue);
			OnChange?.Invoke(oldValue, newValue);
			return oldValue;
		}
		// It would be nice to have an easy way of calling Interlocked.CompareExchange, but it would not make sense to use it when T is a reference type

		public override bool Equals(object obj) => obj is MutableReference<T> otherRef && Equals(_value, otherRef._value);

		public override int GetHashCode() => _value.GetHashCode();

		public override string ToString() => $"<{_value}>";

		public static implicit operator T(MutableReference<T> reference) => reference.Value;
	}
}
