using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using NUnit.Framework;
using SpotifyProject;
using SpotifyProject.Utils.Extensions;

namespace SpotifyProjectTests
{
	public class Benchmarker
	{
		public void RunBenchmark<T>(string testName)
		{
			try
			{
				var summary = BenchmarkRunner.Run<T>();
				Logger.Information($"Benchmark info for test: {testName} is stored in {summary.ResultsDirectoryPath}");
			} catch (Exception e)
			{
				var failureMessage = $"An exception occurred while running benchmark test {testName}: {e}";
				Logger.Error(failureMessage);
				Assert.Fail(failureMessage);
			}
			Assert.Pass();
		}

		public void RunManualBenchmark(string testName, int warmupRuns, int trials, int invocationsPerTrial, object instance)
		{
			Action BuildAction(MethodInfo method)
			{
				return Expression.Lambda<Action>(Expression.Call(Expression.Constant(instance), method)).Compile();
			}
			var instanceType = instance.GetType();
			var methods = instanceType.GetMethods()
				.Where(methodInfo => methodInfo.CustomAttributes.Any(attributeData => attributeData.AttributeType == typeof(BenchmarkAttribute)))
				.OrderBy(methodInfo => methodInfo.MetadataToken);
			var benchmarks = methods.Select(method => (method.Name, BuildAction(method))).ToArray();
			RunManualBenchmark(testName, warmupRuns, trials, invocationsPerTrial, benchmarks);
		}


		public void RunManualBenchmark(string testName, int warmupRuns, int trials, int invocationsPerTrial, params (string funcName, Func<object> action)[] benchmarks) =>
			RunManualBenchmark(testName, warmupRuns, trials, invocationsPerTrial,
				benchmarks.Select<(string funcName, Func<object> action), (string funcName, Action action)>(benchmark => (benchmark.funcName, () => benchmark.action())).ToArray());

		public void RunManualBenchmark(string testName, int warmupRuns, int trials, int invocationsPerTrial, params (string funcName, Action action)[] benchmarks)
		{
			Dictionary<string, TimeSpan> times = new Dictionary<string, TimeSpan>();
			foreach (var (funcName, action) in benchmarks)
			{
				for (int i = 0; i < warmupRuns; i++)
					action();
			}

			var random = new Random();
			var clock = new Stopwatch();
			for (int i = 0; i < trials; i++)
			{
				var order = benchmarks.RandomShuffle(random);
				foreach (var (funcName, action) in order)
				{
					clock.Start();
					for (int j = 0; j < invocationsPerTrial; j++)
					{
						action();
					}
					clock.Stop();
					var elapsed = clock.Elapsed;
					clock.Reset();
					times[funcName] = times.TryGetValue(funcName, out var existingElapsed) ? existingElapsed + elapsed : elapsed;
				}
				if (i == 5)
					Logger.Information($"After 5 trials, average time per trial: {times.Values.Aggregate(TimeSpan.Zero, (t1, t2) => t1 + t2).Divide(5).Milliseconds}ms");
			}
			var maxFuncNameLength = benchmarks.Select(tup => tup.funcName.Length).Max();
			var resultLines = string.Concat(benchmarks
				.Select(benchmark => benchmark.funcName)
				.Select(funcName => $"\n\t{funcName}{new string(' ', maxFuncNameLength + 2 - funcName.Length)}||  {(times[funcName].Milliseconds * 1000000d) / (trials * invocationsPerTrial)}ns"));
			Logger.Information($"Benchmark info for test {testName}:{resultLines}");
		}

		public Type CreateBenchmarkableType(string name, Func<object> func)
		{
			var methodInfo = func.GetMethodInfo();
			var returnType = methodInfo.ReturnType;
			var assemblyName = new AssemblyName("DynamicBenchmarking");
			var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
			var moduleBuilder =	assemblyBuilder.DefineDynamicModule(assemblyName.Name);
			var typeBuilder = moduleBuilder.DefineType("DynamicBencharker", TypeAttributes.Public | TypeAttributes.Class);
			var methodBuilder = typeBuilder.DefineMethod(name, MethodAttributes.Public | MethodAttributes.HideBySig, returnType, Type.EmptyTypes);
			var il = methodBuilder.GetILGenerator();
			//il.Emit(OpCodes.Ldnull);
			//il.Emit(OpCodes.Callvirt, methodInfo);
			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Ldind_Ref, methodInfo);
			il.EmitCalli(OpCodes.Calli, CallingConventions.Standard | CallingConventions.HasThis, typeof(object), Type.EmptyTypes, null);
			il.Emit(OpCodes.Ret);
			return typeBuilder.CreateType();
		}
	}

}
