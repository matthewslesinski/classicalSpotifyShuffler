using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CustomResources.Utils.Concepts.DataStructures;
using CustomResources.Utils.Extensions;

namespace CustomResources.Utils.GeneralUtils
{
	public static class ReflectionUtils<T>
	{
		public static Func<T, R> RetrieveGetterByPropertyName<R>(string propertyName, bool throwOnDoesntExist = true)
		{
			var numProperties = TryRetrieveGetterByPropertyName<R>(propertyName, out var func);
			if (numProperties != 1 && throwOnDoesntExist)
				throw new KeyNotFoundException($"Ambiguous property name for property {propertyName} on type {typeof(T).FullName}. There were {numProperties} options found.");
			return func ?? null;
		}

		private static int TryRetrieveGetterByPropertyName<R>(string propertyName, out Func<T, R> getter)
		{
			getter = null;
			if (string.IsNullOrWhiteSpace(propertyName))
				return 0;
			if (_gettersByPropertyName.TryGetCastedValue(propertyName, out getter))
				return 1;
			
			Func<T, R> result;
			var type = typeof(T);
			var propertyInfos = _propertiesByName[propertyName].ToList();
			if (propertyInfos.Count != 1)
				return propertyInfos.Count;
			ParameterExpression input = Expression.Parameter(type);
			var expr = Expression.Convert(Expression.Property(input, propertyInfos.First()), typeof(R));
			result = Expression.Lambda<Func<T, R>>(expr, input).Compile();
			_gettersByPropertyName[propertyName] = getter = result;
			return 1;
		}

		private readonly static ILookup<string, PropertyInfo> _propertiesByName = typeof(T).GetAllPublicProperties().ToLookup(p => p.Name);

		private readonly static Dictionary<string, object> _gettersByPropertyName = new Dictionary<string, object>();
	}

	public static class ReflectionUtilsOld
	{
		public static IEnumerable<PropertyInfo> GetAllPublicProperties(this Type containingType)
		{
			return GetAllImplementedTypes(containingType).SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance)).Distinct()
				.GroupBy(property => property.Name)
				.Select(propertyGroup =>
				{
					var baseTypes = GetFurthestBaseTypes(propertyGroup.Select(property => property.DeclaringType));
					return propertyGroup.Where(property => baseTypes.Contains(property.DeclaringType));
				})
				.SelectMany(group => group);
		}

		public static ISet<Type> GetFurthestBaseTypes(IEnumerable<Type> originalTypes)
		{
			var baseTypes = new HashSet<Type>();
			foreach (var type in originalTypes.Where(type => type != null))
			{
				baseTypes.RemoveWhere(type.IsAssignableFrom);
				if (!baseTypes.Any(seenType => seenType.IsAssignableFrom(type)))
					baseTypes.Add(type);
			}
			return baseTypes;
		}

		public static IEnumerable<Type> GetAllImplementedTypes(this Type startingType)
		{
			var parents = Utils.TraverseBreadthFirst(startingType, t => t.BaseType != null ? new[] { t.BaseType }.Concat(t.GetInterfaces()) : t.GetInterfaces());
			return parents;
		}

		public static R GetPropertyByName<R>(object containingObject, string propertyName, bool throwOnDoesntExist = true)
		{
			if (!TryGetPropertyByName<R>(containingObject, propertyName, out var output) && throwOnDoesntExist)
				throw new ArgumentException($"Cannot get property \"{propertyName}\" on object of type {containingObject.GetType().FullName}");
			return output;
		}

		public static bool TryGetPropertyByName<R>(object containingObject, string propertyName, out R output)
		{
			output = default;
			if (string.IsNullOrWhiteSpace(propertyName))
				return false;

			var typeT = containingObject.GetType();
			var getter = TryRetrievePropertyGetter(typeT);
			var outputResults = getter(containingObject, propertyName);
			output = (R) outputResults[1];
			return (bool) outputResults[0];
		}

		public static Func<object, string, object[]> TryRetrievePropertyGetter(Type containingObjectType)
		{
			if (!_propertyGettersForTypes.TryGetValue(containingObjectType, out Func<object, string, object[]> getter))
			{
				var methodInfo = typeof(ReflectionUtilsOld).GetMethod(nameof(ReflectionUtilsOld.TryGetPropertyByNameHelper), BindingFlags.NonPublic | BindingFlags.Static);
				var constructedMethodInfo = methodInfo.MakeGenericMethod(containingObjectType);

				var objectInput = Expression.Parameter(typeof(object));
				var propertyNameInput = Expression.Parameter(typeof(string));
				var callExpression = Expression.Call(constructedMethodInfo, objectInput.Cast(containingObjectType), propertyNameInput);

				var lambdaExpr = Expression.Lambda<Func<object, string, object[]>>(callExpression, objectInput, propertyNameInput);

				_propertyGettersForTypes[containingObjectType] = getter = lambdaExpr.Compile();
			}
			return getter;
		}

		private static object[] TryGetPropertyByNameHelper<T>(T containingObject, string propertyName)
		{
			return new[] { TryGetPropertyByName<T, object>(containingObject, propertyName, out var output), output };
		}


		public static bool TryGetPropertyByName<T, R>(T containingObject, string propertyName, out R output)
		{
			if (TryRetrieveGetterByPropertyName<T, R>(propertyName, out var getter))
			{
				output = getter(containingObject);
				return true;
			}
			output = default;
			return false;
		}

		public static bool TryRetrieveGetterByPropertyName<T, R>(string propertyName, out Func<T, R> getter)
		{
			getter = ReflectionUtils<T>.RetrieveGetterByPropertyName<R>(propertyName, false);
			return getter != null;
		}

		private readonly static Dictionary<Type, Func<object, string, object[]>> _propertyGettersForTypes = new Dictionary<Type, Func<object, string, object[]>>();
	}

	public static class ReflectionUtils
	{
		public static R GetPropertyByName<R>(this object containingObject, string propertyName, bool throwOnDoesntExist = true)
		{
			var foundGetter = RetrieveGetterByPropertyName<R>(containingObject.GetType(), propertyName, throwOnDoesntExist);
			return foundGetter == null ? default : foundGetter(containingObject);
		}

		public static bool TryGetPropertyByName<R>(this object containingObject, string propertyName, out R output)
		{
			var foundGetter = RetrieveGetterByPropertyName<R>(containingObject.GetType(), propertyName, false);
			if (foundGetter == null)
			{
				output = default;
				return false;
			}
			output = foundGetter(containingObject);
			return true;
		}

		public static Func<T, R> RetrieveGetterByPropertyName<T, R>(string propertyName, bool throwOnDoesntExist = true)
		{
			var objectType = typeof(T);
			var numProperties = TryRetrieveGetterByPropertyName<R>(objectType, propertyName, out var func);
			if (numProperties != 1 && throwOnDoesntExist)
				throw new KeyNotFoundException($"Ambiguous or unknown property name for property {propertyName} on type {objectType.FullName}. There were {numProperties} properties with that name found.");
			return func == null ? null : t => func(t);
		}

		public static Func<object, R> RetrieveGetterByPropertyName<R>(Type objectType, string propertyName, bool throwOnDoesntExist = true)
		{
			var numProperties = TryRetrieveGetterByPropertyName<R>(objectType, propertyName, out var func);
			if (numProperties != 1 && throwOnDoesntExist)
				throw new KeyNotFoundException($"Ambiguous or unknown property name for property {propertyName} on type {objectType.FullName}. There were {numProperties} properties with that name found.");
			return func;
		}

		private static int TryRetrieveGetterByPropertyName<R>(Type objectType, string propertyName, out Func<object, R> getter)
		{
			getter = null;
			if (string.IsNullOrWhiteSpace(propertyName))
				return 0;
			var key = propertyName;
			var propertyDictionary = _propertyGettersForTypes.AddIfNotPresent(objectType, type => new Dictionary<string, Delegate>());
			if (propertyDictionary.TryGetCastedValue(key, out getter))
				return 1;

			var propertyInfos = _propertyInfosForTypes.AddIfNotPresent(objectType, type => type.GetAllPublicProperties().ToLookup(p => p.Name))[propertyName].ToList();
			if (propertyInfos.Count != 1)
				return propertyInfos.Count;

			ParameterExpression input = Expression.Parameter(typeof(object));
			var expr = Expression.Property(Expression.Convert(input, objectType), propertyInfos.Single());
			var result = Expression.Lambda(expr, input).Compile();
			getter = (Func<object, R>) (propertyDictionary[key] = result);
			return 1;
		}

		private readonly static IDictionary<Type, Dictionary<string, Delegate>> _propertyGettersForTypes = new UniqueKeyDictionary<Type, Dictionary<string, Delegate>>();

		private readonly static IDictionary<Type, ILookup<string, PropertyInfo>> _propertyInfosForTypes = new Dictionary<Type, ILookup<string, PropertyInfo>>();

		private class PropertyNameEqualityComparer : IEqualityComparer<string>
		{
			internal PropertyNameEqualityComparer(IEnumerable<string> propertyNames)
			{
				//TODO
			}

			public bool Equals(string x, string y)
			{
				return string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
			}

			public int GetHashCode([DisallowNull] string obj)
			{
				throw new NotImplementedException();
			}
		}
	}
}

