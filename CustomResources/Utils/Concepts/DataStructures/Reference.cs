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

	public abstract class MutableReference<T, ContainerT> : IWrapper<T> where ContainerT : class
	{
		public delegate void OnChangeAction(T oldValue, T newValue);

		protected ContainerT _value;
		public MutableReference(T value)
		{
			_value = ToContainer(value);
		}

		public event OnChangeAction OnChange;

		public T WrappedObject => FromContainer(_value);
		public T Value { get => FromContainer(_value); set => Change(value); }
		public T ValueContainer => FromContainer(_value);

		public T Change(T newValue)
		{
			var oldValue = FromContainer(_value);
			_value = ToContainer(newValue);
			OnChange?.Invoke(oldValue, newValue);
			return oldValue;
		}

		public T AtomicExchange(T newValue)
		{
			var oldValue = FromContainer(Interlocked.Exchange(ref _value, ToContainer(newValue)));
			OnChange?.Invoke(oldValue, newValue);
			return oldValue;
		}
		// It would be nice to have an easy way of calling Interlocked.CompareExchange, but it would not make sense to use it when T is a reference type

		public override bool Equals(object obj) => obj is MutableReference<T, ContainerT> otherRef && Equals(_value, otherRef._value);

		public override int GetHashCode() => _value.GetHashCode();

		public override string ToString() => $"<{_value}>";

		public static implicit operator T(MutableReference<T, ContainerT> reference) => reference.Value;

		protected void OnChangeInvoke(T oldValue, T newValue) => OnChange?.Invoke(oldValue, newValue);

		protected abstract T FromContainer(ContainerT container);
		protected abstract ContainerT ToContainer(T value);
	}

	public class MutableReference<T> : MutableReference<T, Reference<T>>
	{
		public MutableReference(T value) : base(value)
		{ }

		protected override T FromContainer(Reference<T> container) => container;
		protected override Reference<T> ToContainer(T value) => value;
	}

	public class MutableClassReference<T> : MutableReference<T, T> where T : class
	{
		public MutableClassReference(T value) : base(value)
		{ }

		protected override T FromContainer(T value) => value;
		protected override T ToContainer(T value) => value;

		public T AtomicCompareExchange(T newValue, T comparand)
		{
			var oldValue = Interlocked.CompareExchange(ref _value, newValue, comparand);
			if (oldValue == comparand && oldValue != newValue)
				OnChangeInvoke(oldValue, newValue);
			return oldValue;
		}
	}
}
