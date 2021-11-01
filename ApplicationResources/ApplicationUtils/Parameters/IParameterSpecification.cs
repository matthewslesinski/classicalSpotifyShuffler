using System;
using ApplicationResources.Setup;
using CustomResources.Utils.Extensions;

namespace ApplicationResources.ApplicationUtils.Parameters
{
	public interface IParameterSpecification : ISettingSpecification
	{
		Type Type { get; }
		bool IsValueAllowed(object value);
	}

	public interface IParameterSpecification<in T> : IParameterSpecification
	{
		bool IsValueAllowed(T value);
		bool IParameterSpecification.IsValueAllowed(object value)
		{
			var allowedType = Type;
			if (value is null)
			{
				// Note that default should always end up being null, but the compiler doesn't know that
				return !allowedType.IsValueType && IsValueAllowed(default);
			}
			var valueType = value.GetType();
			if (!valueType.IsAssignableTo(allowedType))
				return false;
			var castedValue = value.AsUnsafe<T>();
			return IsValueAllowed(castedValue);
		}
	}

	public class ParameterSpecification<T> : SettingSpecification<T>, IParameterSpecification<T>
	{
		public bool IsValueAllowed(T value) => Validator == null || Validator(value);
	}
}
