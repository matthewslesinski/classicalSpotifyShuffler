using System;
using System.Linq.Expressions;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;
using Converter = System.Convert;

namespace CustomResources.Utils.Concepts
{
	public class Bijection<S, T>
	{
		public Func<S, T> Function { get; }
		public Func<T, S> Inverse { get; }

		public Bijection(Func<S, T> function, Func<T, S> inverse)
		{
			Ensure.ArgumentNotNull(function, nameof(function));
			Ensure.ArgumentNotNull(inverse, nameof(inverse));

			this.Function = function;
			this.Inverse = inverse;
		}

		public T Invoke(S arg) => Function(arg);
		public S InvokeInverse(T arg) => Inverse(arg);

		public Bijection<S, R> AndThen<R>(Bijection<T, R> next) => new Bijection<S, R>(Function.AndThen(next.Function), next.Inverse.AndThen(Inverse));
	}

	public static class Bijections
	{
		public static Bijection<S, T> Convert<S, T>() where S : IConvertible where T : IConvertible => new Bijection<S, T>(s => (T)Converter.ChangeType(s, typeof(T)), t => (S)Converter.ChangeType(t, typeof(S)));

		public static Bijection<T, T> Add<T>(T addValue) where T : IConvertible => FromBinaryOperator<T, T>(addValue, Expression.Add, Expression.Subtract);
		public static Bijection<T, T> Multiply<T>(T addValue) where T : IConvertible => FromBinaryOperator<T, T>(addValue, Expression.Multiply, Expression.Divide);
		public static Bijection<double, double> Power(double power) => FromBinaryOperator<double, double>(power, Expression.Power, (inputExpr, powerExpr) => Expression.Power(inputExpr, Expression.Divide(Expression.Constant(1d), powerExpr)));

		public static Bijection<S, T> FromBinaryOperator<S, T>(object seed, Func<Expression, Expression, Expression> op, Func<Expression, Expression, Expression> inverse) =>
			FromUnaryOperator<S, T>(paramExpr => op(paramExpr, Expression.Constant(seed)), paramExpr => inverse(paramExpr, Expression.Constant(seed)));

		public static Bijection<S, T> FromUnaryOperator<S, T>(Func<Expression, Expression> op, Func<Expression, Expression> inverse)
		{
			var inputParam = Expression.Parameter(typeof(S));
			var inverseInputParam = Expression.Parameter(typeof(T));
			var opExpr = op(inputParam);
			var inverseExpr = inverse(inverseInputParam);
			return new Bijection<S, T>(Expression.Lambda<Func<S, T>>(opExpr, inputParam).Compile(), Expression.Lambda<Func<T, S>>(inverseExpr, inverseInputParam).Compile());
		}
	}

	public static class Bijections<T>
	{
		public static readonly Bijection<T, T> Identity = new Bijection<T, T>(i => i, i => i);
	}
}
