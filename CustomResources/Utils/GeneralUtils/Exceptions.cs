using System;
namespace CustomResources.Utils.GeneralUtils
{
	public static class Exceptions
	{
		public static R ThrowMemberNotFound<R, B>(string memberName) => ThrowMemberNotFound<R>(memberName, typeof(B).Name);
		public static R ThrowMemberNotFound<R>(string memberName, string baseTypeName = null) =>
			Throw<R>(new MissingMemberException($"The attempt to access member {memberName} {(baseTypeName == null ? "" : $"on type {baseTypeName} ")}failed because the member does not exist"));

		public static R Throw<R>() => Throw<R>(new Exception());
		public static R Throw<R, E>() where E : Exception, new() => Throw<R>(new E());
		public static R Throw<R>(string message, Func<string, Exception> exceptionConstructor = null) => Throw<R>((exceptionConstructor ?? (m => new Exception(m)))(message));
		public static R Throw<R>(Exception e) => throw e;
	}
}
