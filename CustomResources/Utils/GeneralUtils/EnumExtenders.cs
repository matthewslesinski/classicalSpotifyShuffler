using System;
using System.Collections.Generic;
using System.Linq;
using CustomResources.Utils.Extensions;

namespace CustomResources.Utils.GeneralUtils
{
	public static class EnumExtenders<ExtensionT>
	{
		private readonly static IDictionary<Enum, ExtensionT> _specificationMapping = new Dictionary<Enum, ExtensionT>();
		private readonly static IDictionary<Type, IEnumerable<EnumExtensionProviderAttribute>> _extensionProviderAttributes = new Dictionary<Type, IEnumerable<EnumExtensionProviderAttribute>>();

		public static ExtensionT GetEnumExtension(Enum enumValue) => _specificationMapping.TryGetValue(enumValue, out var foundExtension)
			? foundExtension
			: AddNewEnums(enumValue.GetType())[enumValue];

		private static IDictionary<Enum, ExtensionT> AddNewEnums(Type enumType)
		{
			var enumValues = Enum.GetValues(enumType).Cast<Enum>();
			var extensionType = typeof(ExtensionT);
			var attributes = FindExtensionProviderAttributes(enumType);
			if (attributes.Any())
				attributes.Single().Instance.AsUnsafe<IGenericEnumExtensionProvider<ExtensionT>>().GetPairs().Each(pair => _specificationMapping.Add(pair.enumValue, pair.enumExtension));
			else if (extensionType == typeof(EmptyEnumExtension))
				enumValues.Each(val => _specificationMapping.Add(val, new EmptyEnumExtension().AsUnsafe<ExtensionT>()));
			else
				throw new NotSupportedException($"There is no IEnumExtensionProvider designated to extend enums of type {enumType.Name} with extensions of type {extensionType.Name}");
			var unImplementedEnums = enumValues.Except(_specificationMapping.Keys);
			if (unImplementedEnums.Any())
				throw new NotImplementedException($"A specification of type {extensionType.Name} was not provided for the following enums of type {enumType.Name}: " +
					$"{string.Join(", ", unImplementedEnums)}");
			return _specificationMapping;
		}

		public static IEnumerable<EnumExtensionProviderAttribute> FindExtensionProviderAttributes(Type enumType)
		{
			return _extensionProviderAttributes.AddIfNotPresent(enumType, type =>
				type.GetCustomAttributes(typeof(EnumExtensionProviderAttribute), false)
					.Cast<EnumExtensionProviderAttribute>()
					.Where(attribute => attribute.EnumType == enumType && attribute.ExtensionType.IsAssignableTo(typeof(ExtensionT))));
		}
	}

	public interface IGenericEnumExtensionProvider<ExtensionT>
	{
		internal IEnumerable<(Enum enumValue, ExtensionT enumExtension)> GetPairs();
	}

	public interface IEnumExtensionProvider<EnumT, ExtensionT> : IGenericEnumExtensionProvider<ExtensionT> where EnumT : struct, Enum
	{
		IEnumerable<(Enum enumValue, ExtensionT enumExtension)> IGenericEnumExtensionProvider<ExtensionT>.GetPairs() => Specifications.Select(pair => ((Enum) pair.Key, pair.Value));

		IReadOnlyDictionary<EnumT, ExtensionT> Specifications { get; }
	}

	public struct EmptyEnumExtension { }

	[AttributeUsage(AttributeTargets.Enum, AllowMultiple = true)]
	public class EnumExtensionProviderAttribute : Attribute
	{
		public Type EnumType { get; }
		public Type ExtensionType { get; }
		public Type ProviderType { get; }
		public object Instance { get; }


		public EnumExtensionProviderAttribute(Type providerType)
		{
			if (!providerType.IsClass)
				throw new ArgumentException("The enum extension provider type must be a class");
			var interfaceType = providerType
				.FindInterfaces((interfaceType, criteria) => interfaceType.GetGenericTypeDefinition() == (Type) criteria, typeof(IEnumExtensionProvider<,>))
				.Single();
			var constructor = providerType.GetConstructor(Array.Empty<Type>());
			if (constructor == null)
				throw new ArgumentException("The enum extension provider type must have an no argument constructor");
			var genericTypeArgs = interfaceType.GetGenericArguments();
			Instance = Activator.CreateInstance(providerType);
			EnumType = genericTypeArgs[0];
			ExtensionType = genericTypeArgs[1];
			ProviderType = providerType;
		}
	}
}
