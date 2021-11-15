using System;
using System.Collections.Generic;
using System.Linq;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;
using ApplicationExtensions = ApplicationResources.Utils.GeneralExtensions;

namespace ApplicationResources.Setup
{
	public delegate object GeneralValueGetter(IEnumerable<string> values);
	public delegate T ValueGetter<out T>(IEnumerable<string> values);
	public delegate string GeneralStringFormatter(object obj);
	public delegate string StringFormatter<in T>(T obj);

	public interface ISettingSpecification
	{
		bool IsRequired { get; set; }
		bool HasDefault { get; }
		object Default { get; set; }
		GeneralValueGetter ValueGetter { get; set; }
		GeneralStringFormatter StringFormatter { get; set; }
		bool IsValueAllowed(object value);
	}

	public interface IParameterSpecification : ISettingSpecification
	{
		Type Type { get; }
	}

	public interface IParameterSpecification<in T> : IParameterSpecification
	{
		bool IsValueAllowed(T value);
	}

	public abstract class SettingSpecification : ISettingSpecification
	{
		public bool IsRequired { get; set; } = false;

		public GeneralValueGetter ValueGetter { get; set; } = rawValues => throw new NotImplementedException("A parse function was not provided");

		public GeneralStringFormatter StringFormatter { get; set; } = obj => obj?.ToString();

		public bool HasDefault { get; set; } = false;
		public object Default { get => _default; set { HasDefault = true; _default = value; } }
		private object _default = null;

		public abstract bool IsValueAllowed(object value);
	}

	public class SettingSpecification<T> : SettingSpecification
	{
		public new T Default { set => base.Default = value; }
		public new StringFormatter<T> StringFormatter
		{
			set
			{
				Ensure.ArgumentNotNull(value, nameof(StringFormatter));
				base.StringFormatter = obj => obj is T castedObj
					? value(castedObj)
					: (typeof(T).SupportsNullValues() && obj is null)
						? value((T)obj)
						: Exceptions.Throw<string>(new ArgumentException($"This string formatter can only be applied to arguments of type {typeof(T).Name}, " +
							$"but received input of type {obj?.GetType().Name}"));
			}
		}
		public new ValueGetter<T> ValueGetter
		{
			set
			{
				Ensure.ArgumentNotNull(value, nameof(ValueGetter));
				base.ValueGetter = values =>
				{
					var parsedValue = value(values);
					if (!IsValueAllowed(parsedValue))
						throw new ArgumentException($"The supplied value did not pass validation {values}");
					return parsedValue;
				};
			}
		}
		public Type Type { get; } = typeof(T);
		public Func<T, bool> Validator { get; set; }

		public override bool IsValueAllowed(object value)
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

		public bool IsValueAllowed(T value) => Validator == null || Validator(value);
	}

	public class ParameterSpecification<T> : SettingSpecification<T>, IParameterSpecification<T>
	{
	}

	public class MultipleValuesSettingSpecification<T> : ParameterSpecification<IEnumerable<T>>
	{
		public MultipleValuesSettingSpecification()
		{
			StringFormatter = ApplicationExtensions.ToJsonString;
		}
	}

	public class StringSettingSpecification : ParameterSpecification<string>
	{
		public StringSettingSpecification()
		{
			ValueGetter = rawValues => rawValues.TryGetSingle(out var foundResult) && !string.IsNullOrWhiteSpace(foundResult) ? foundResult : default;
		}
	}

	public class MultipleStringsSettingSpecification : MultipleValuesSettingSpecification<string>
	{
		public MultipleStringsSettingSpecification()
		{
			ValueGetter = values => values;
		}
	}

	public class ConvertibleSettingSpecification<T> : ParameterSpecification<T> where T : struct, IConvertible
	{
		internal static readonly ValueGetter<T> DefaultValueGetter = values => (T)Convert.ChangeType(values.Single(), typeof(T));
		public ConvertibleSettingSpecification()
		{
			ValueGetter = DefaultValueGetter;
		}
	}

	public class NullableConvertibleSettingSpecification<T> : ParameterSpecification<T?> where T : struct, IConvertible
	{
		public NullableConvertibleSettingSpecification()
		{
			ValueGetter = values => values.Any() ? ConvertibleSettingSpecification<T>.DefaultValueGetter(values) : null;
		}
	}

	public class BoolSettingSpecification : ParameterSpecification<bool>
	{
		public BoolSettingSpecification()
		{
			Default = false;
			ValueGetter = values => !values.TryGetSingle(out var singleValue) || !bool.TryParse(singleValue, out var parsedValue) || parsedValue;
		}
	}

	public class EnumSettingSpecification<T> : ParameterSpecification<T> where T : struct, Enum
	{
		public EnumSettingSpecification()
		{
			ValueGetter = values => Enum.Parse<T>(values.Single(), true);
		}
	}
}
