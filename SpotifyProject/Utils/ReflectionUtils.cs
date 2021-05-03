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
			return RetrieveGetterByPropertyName(propertyName, throwOnDoesntExist ? (Func<T, R>) null : t => default);
		}

		public static Func<T, R> RetrieveGetterByPropertyName<R>(string propertyName, Func<T, R> defaultGetter)
		{
			if (_gettersByPropertyName.TryGetValue(propertyName, out var getterObject))
				return (Func<T, R>) getterObject;
			else
			{
				Func<T, R> result;
				var type = typeof(T);
				var propertyInfos = _propertiesByName[propertyName];
				if (defaultGetter != null && propertyInfos.Count() != 1) {
					return defaultGetter;
				} else
				{
					ParameterExpression input = Expression.Parameter(type);
					if (propertyInfos.Count() != 1)
						throw new ArgumentException($"Ambiguous property name for property {propertyName} on type {type.FullName}. There were {propertyInfos.Count()} options found.");
					var expr = Expression.Convert(Expression.Property(input, propertyInfos.First()), typeof(R));
					result = Expression.Lambda<Func<T, R>>(expr, input).Compile();
				}
				_gettersByPropertyName[propertyName] = result;
				return result;
			}
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
