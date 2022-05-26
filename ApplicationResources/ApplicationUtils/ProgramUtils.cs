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
				Settings.RegisterProvider(new CommandLineSettingsProvider(app));
				Settings.RegisterSettings<BasicSettings>();
				startupArgs.SettingsTypes.Each(Settings.RegisterSettings);
				startupArgs.ParameterTypes.Each(TaskParameters.RegisterParameters);
				Func<CancellationToken, Task> runner = ((Func<CancellationToken, Task>)Settings.Load).FollowedByAsync(program).AndThenAsync(OnTerminate);
				if (!string.IsNullOrWhiteSpace(startupArgs.XmlSettingsFileFlag))
				{
					var xmlSettingsFileOption = app.Option(startupArgs.XmlSettingsFileFlag, "The file name for settings stored in xml format", CommandOptionType.MultipleValue);
					Func<CancellationToken, Task> settingsProviderRegister = async cancellationToken =>
					{
						var localData = GlobalDependencies.GlobalDependencyContainer.GetRequiredService<IDataStoreAccessor>();
						if (xmlSettingsFileOption.HasValue())
						{
							var existingFiles = await xmlSettingsFileOption.Values.WhereAsync(localData.ExistsAsync, cancellationToken).ToList().WithoutContextCapture();
							Settings.RegisterProviders(existingFiles.Select(fileName => new XmlSettingsProvider(fileName)));
						}
						if (startupArgs.AdditionalXmlSettingsFiles.Any())
						{
							var existingFiles = await startupArgs.AdditionalXmlSettingsFiles.WhereAsync(localData.ExistsAsync, cancellationToken).ToList().WithoutContextCapture();
							Settings.RegisterProviders(existingFiles.Select(fileName => new XmlSettingsProvider(fileName)));
						}
						if (await localData.ExistsAsync(ApplicationConstants.StandardSettingsFile).WithoutContextCapture())
							Settings.RegisterProvider(new XmlSettingsProvider(ApplicationConstants.StandardSettingsFile));
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
				var localData = GlobalDependencies.GlobalDependencyContainer.GetRequiredService<IDataStoreAccessor>();
				Settings.RegisterSettings<BasicSettings>();
				startupArgs.SettingsTypes.Each(Settings.RegisterSettings);
				startupArgs.ParameterTypes.Each(TaskParameters.RegisterParameters);
				if (startupArgs.AdditionalXmlSettingsFiles.Any())
				{
					var existingFiles = await startupArgs.AdditionalXmlSettingsFiles.WhereAsync(localData.ExistsAsync).ToList().WithoutContextCapture();
					Settings.RegisterProviders(existingFiles.Select(fileName => new XmlSettingsProvider(fileName)));
				}
				if (await localData.ExistsAsync(ApplicationConstants.StandardSettingsFile).WithoutContextCapture())
					Settings.RegisterProvider(new XmlSettingsProvider(ApplicationConstants.StandardSettingsFile));
				await Settings.Load().WithoutContextCapture();
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

		public class StartupArgs
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
