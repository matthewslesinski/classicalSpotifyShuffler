using System;
using System.Linq.Expressions;

namespace SpotifyProject.Utils.GeneralUtils
{
	public static class ExpressionUtils
	{
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

		public static Expression Cast(this Expression expr, Type castType)
		{
			var tempVariable = Expression.Variable(expr.Type);
			var condition = Expression.TypeIs(tempVariable, castType);
			var castedExpr = Expression.Convert(tempVariable, castType);
			var throwsExpr = Expression.Throw(Expression.Constant(new Exception($"Cannot cast expression of type {expr.Type} to {castType}")), castType);
			return Expression.Block(new[] { tempVariable },
				Expression.Assign(tempVariable, expr),
				Expression.Condition(condition,
					castedExpr,
					throwsExpr));
		}
	}
}
