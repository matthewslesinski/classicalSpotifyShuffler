using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SpotifyProject.Utils
{
	public static class ReflectionUtils<T>
	{

		public static Func<T, R> RetrieveGetterByPropertyName<R>(string propertyName, bool throwOnDoesntExist = true)
		{
			var numProperties = TryRetrieveGetterByPropertyName<R>(propertyName, out var func);
			if (numProperties != 1 && throwOnDoesntExist)
				throw new ArgumentException($"Ambiguous property name for property {propertyName} on type {typeof(T).FullName}. There were {numProperties} options found.");
			return func ?? (t => default);
		}

		public static Func<T, R> RetrieveGetterByPropertyName<R>(string propertyName, Func<T, R> defaultGetter)
		{
			return TryRetrieveGetterByPropertyName<R>(propertyName, out var func) == 1 ? func : defaultGetter;
		}

		private static int TryRetrieveGetterByPropertyName<R>(string propertyName, out Func<T, R> getter)
		{
			getter = null;
			if (string.IsNullOrWhiteSpace(propertyName))
				return 0;
			if (_gettersByPropertyName.TryGetValue(propertyName, out var getterObject))
			{
				getter = (Func<T, R>) getterObject;
				return 1;
			}
			Func<T, R> result;
			var type = typeof(T);
			var propertyInfos = _propertiesByName[propertyName].ToList();
			if (propertyInfos.Count() != 1)
				return propertyInfos.Count();
			ParameterExpression input = Expression.Parameter(type);
			var expr = Expression.Convert(Expression.Property(input, propertyInfos.First()), typeof(R));
			result = Expression.Lambda<Func<T, R>>(expr, input).Compile();
			_gettersByPropertyName[propertyName] = getter = result;
			return 1;
			
		}

		private static IEnumerable<PropertyInfo> GetAllPublicProperties()
		{
			return GetAllBaseTypes().SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance)).Distinct();
		}

		private static IEnumerable<Type> GetAllBaseTypes()
		{

			var startingType = typeof(T);
			var parents = Utils.TraverseBreadthFirst(startingType, t => t.BaseType != null ? new[] { t.BaseType }.Concat(t.GetInterfaces()) : t.GetInterfaces());
			return parents;
		}

		private readonly static ILookup<string, PropertyInfo> _propertiesByName = GetAllPublicProperties().ToLookup(p => p.Name);

		private readonly static Dictionary<string, object> _gettersByPropertyName = new Dictionary<string, object>();
	}
}
