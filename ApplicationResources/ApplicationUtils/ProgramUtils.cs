using System;
using System.IO;
using System.Linq;
using ApplicationResources.Logging;
using McMaster.Extensions.CommandLineUtils;
using ApplicationResources.Setup;
using System.Collections.Generic;
using CustomResources.Utils.Extensions;
using System.Threading.Tasks;
using System.Threading;
using ApplicationResources.ApplicationUtils.Parameters;
using CustomResources.Utils.GeneralUtils;
using ApplicationResources.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ApplicationResources.ApplicationUtils
{
	public static class ProgramUtils
	{
		public static Task<int> ExecuteCommandLineProgram(Func<Task> program, StartupArgs startupArgs, Func<IDisposable> dependencyInitializer = null)
		{
			Ensure.ArgumentNotNull(program, nameof(program));
			return ExecuteCommandLineProgram(_ => program(), startupArgs, dependencyInitializer);
		}

		public static async Task<int> ExecuteCommandLineProgram(Func<CancellationToken, Task> program, StartupArgs startupArgs, Func<IDisposable> dependencyInitializer = null)
		{
			Ensure.ArgumentNotNull(program, nameof(program));
			Ensure.ArgumentNotNull(startupArgs, nameof(startupArgs));

			AppDomain.CurrentDomain.UnhandledException += (sender, args) => Logger.Error($"An Exception occurred: {args.ExceptionObject}");
			var dependencyDisposable = dependencyInitializer?.Invoke();
			try
			{
				var app = new CommandLineApplication();
				await TaskParameters.Initialize().WithoutContextCapture();
				await Settings.RegisterProvider(new CommandLineSettingsProvider(app)).WithoutContextCapture();
				Settings.RegisterSettings<BasicSettings>();
				startupArgs.SettingsTypes.Each(Settings.RegisterSettings);
				startupArgs.ParameterTypes.Each(TaskParameters.RegisterParameters);
				Func<CancellationToken, Task> runner = ((Func<CancellationToken, Task>)Settings.Load)
					.FollowedByAsync(LoggerConfigurationProvider.InitializeAsync)
					.FollowedByAsync(program)
					.AndThenAsync(OnTerminate);
				if (!string.IsNullOrWhiteSpace(startupArgs.XmlSettingsFileFlag))
				{
					var xmlSettingsFileOption = app.Option(startupArgs.XmlSettingsFileFlag, "The file name for settings stored in xml format", CommandOptionType.MultipleValue);
					Func<CancellationToken, Task> settingsProviderRegister = async cancellationToken =>
					{
						var localData = GlobalDependencies.Get<IDataStoreAccessor>();
						if (xmlSettingsFileOption.HasValue())
						{
							var existingFiles = await xmlSettingsFileOption.Values.WhereAsync(localData.ExistsAsync, cancellationToken).ToList(cancellationToken).WithoutContextCapture();
							await Settings.RegisterProviders(existingFiles.Select(fileName => new XmlSettingsProvider(fileName))).WithoutContextCapture();
						}
						if (startupArgs.AdditionalXmlSettingsFiles.Any())
						{
							var existingFiles = await startupArgs.AdditionalXmlSettingsFiles.WhereAsync(localData.ExistsAsync, cancellationToken).ToList(cancellationToken).WithoutContextCapture();
							await Settings.RegisterProviders(existingFiles.Select(fileName => new XmlSettingsProvider(fileName))).WithoutContextCapture();
						}
						if (await localData.ExistsAsync(ApplicationConstants.StandardSettingsFile, cancellationToken).WithoutContextCapture())
							await Settings.RegisterProvider(new XmlSettingsProvider(ApplicationConstants.StandardSettingsFile)).WithoutContextCapture();
					};
					runner = settingsProviderRegister.FollowedByAsync(runner);
				}
				app.OnExecuteAsync(async cancellationToken => { await runner(cancellationToken).WithoutContextCapture(); return 0; });
				return await app.ExecuteAsync(startupArgs.CommandLineArgs).WithoutContextCapture();
			}
			finally
			{
				dependencyDisposable?.Dispose();
			}
		}

		public static async Task ExecuteProgram(Func<Task> program, StartupArgs startupArgs, Func<IDisposable> dependencyInitializer = null)
		{
			Ensure.ArgumentNotNull(program, nameof(program));
			Ensure.ArgumentNotNull(startupArgs, nameof(startupArgs));
			AppDomain.CurrentDomain.UnhandledException += (sender, args) => Logger.Error($"An Exception occurred: {args.ExceptionObject}");
			var dependencyDisposable = dependencyInitializer?.Invoke();
			try
			{
				var localData = GlobalDependencies.Get<IDataStoreAccessor>();
				await TaskParameters.Initialize().WithoutContextCapture();
				Settings.RegisterSettings<BasicSettings>();
				startupArgs.SettingsTypes.Each(Settings.RegisterSettings);
				startupArgs.ParameterTypes.Each(TaskParameters.RegisterParameters);
				if (startupArgs.AdditionalXmlSettingsFiles.Any())
				{
					var existingFiles = await startupArgs.AdditionalXmlSettingsFiles.WhereAsync(localData.ExistsAsync).ToList().WithoutContextCapture();
					await Settings.RegisterProviders(existingFiles.Select(fileName => new XmlSettingsProvider(fileName))).WithoutContextCapture();
				}
				if (await localData.ExistsAsync(ApplicationConstants.StandardSettingsFile).WithoutContextCapture())
					await Settings.RegisterProvider(new XmlSettingsProvider(ApplicationConstants.StandardSettingsFile)).WithoutContextCapture();
				await Settings.Load().WithoutContextCapture();
				await LoggerConfigurationProvider.InitializeAsync().WithoutContextCapture();
				await program().WithoutContextCapture();
				OnTerminate();
			}
			finally
			{
				dependencyDisposable?.Dispose();
			}
		}

		private static void OnTerminate()
		{
			Logger.Information("Terminating successfully");
			Console.WriteLine("Terminating successfully");
		}

		public struct StartupArgs
		{
			public string[] CommandLineArgs { get; }
			public string XmlSettingsFileFlag { get; set; } = null;
			public IEnumerable<Type> SettingsTypes { get; set; } = Array.Empty<Type>();
			public IEnumerable<Type> ParameterTypes { get; set; } = Array.Empty<Type>();
			public IEnumerable<string> AdditionalXmlSettingsFiles { get; set; } = Array.Empty<string>();

			public StartupArgs(string[] commandLineArgs)
			{
				CommandLineArgs = commandLineArgs;
			}
		}
	}
}
