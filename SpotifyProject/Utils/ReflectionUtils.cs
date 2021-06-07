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
				throw new KeyNotFoundException($"Ambiguous property name for property {propertyName} on type {typeof(T).FullName}. There were {numProperties} options found.");
			return func ?? null;
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
			var allProperties = GetAllBaseTypes().SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance)).Distinct();
			var baseTypes = ReflectionUtils.GetBaseTypes(allProperties.Select(property => property.DeclaringType));
			return allProperties.Where(property => baseTypes.Contains(property.DeclaringType));
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

	public static class ReflectionUtils
	{

		public static ISet<Type> GetBaseTypes(IEnumerable<Type> originalTypes)
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

		public static Expression EvaluateIfNotNull(this Expression inputExpr, Func<Expression, Expression> selector)
		{
			var nullExpression = Expression.Constant(null, inputExpr.Type);
			var tempVariable = Expression.Variable(inputExpr.Type);
			var evaluatedExpression = selector(tempVariable);
			var defaultExpression = Expression.Default(evaluatedExpression.Type);
			var blockExpression = Expression.Block(new[] { tempVariable },
					Expression.Assign(tempVariable, inputExpr),
					Expression.Condition(Expression.NotEqual(nullExpression, tempVariable),
						evaluatedExpression,
						defaultExpression));
			return blockExpression;
		}

		public static R GetPropertyByName<R>(object containingObject, string propertyName, bool throwOnDoesntExist = true)
		{
			if (!TryGetPropertyByName<R>(containingObject, propertyName, out var output) && throwOnDoesntExist)
				throw new ArgumentException($"Cannot get property \"{propertyName}\" on object of type {containingObject.GetType().FullName}");
			return output;
		}

		public static bool TryGetPropertyByName<R>(this object containingObject, string propertyName, out R output)
		{
			output = default;
			if (string.IsNullOrWhiteSpace(propertyName))
				return false;
			
			var typeT = containingObject.GetType();
			Func<string, Func<object, object>> getter = null;
			if (_gettersForTypes.TryGetValue(typeT, out var getterObject))
				getter = getterObject;
			else
			{
				var reflectionUtilsType = typeof(ReflectionUtils<>).MakeGenericType(typeT);
				var methodInfo = reflectionUtilsType.GetMethod(nameof(ReflectionUtils<object>.RetrieveGetterByPropertyName), BindingFlags.Public | BindingFlags.Static);
				var constructedMethodInfo = methodInfo.MakeGenericMethod(typeof(object));

				var objectInput = Expression.Parameter(typeof(object));
				var propertyNameInput = Expression.Parameter(propertyName.GetType());
				var throwOnDoesntExistInput = Expression.Constant(false);

				var getterRetriever = Expression.Call(constructedMethodInfo, propertyNameInput, throwOnDoesntExistInput);

				var castExpression = Expression.TypeAs(objectInput, typeT);
				var innerLambda = getterRetriever.EvaluateIfNotNull(getter => Expression.Lambda<Func<object, object>>(Expression.Invoke(getter, castExpression), objectInput));

				_gettersForTypes[typeT] = getter = Expression.Lambda<Func<string, Func<object, object>>>(innerLambda, propertyNameInput).Compile();
			}
			var propertyGetter = getter(propertyName);
			if (propertyGetter != null)
			{
				output = (R) propertyGetter(containingObject);
				return true;
			}
			return false;
		}

		private readonly static Dictionary<Type, Func<string, Func<object, object>>> _gettersForTypes = new Dictionary<Type, Func<string, Func<object, object>>>();
	}
}
