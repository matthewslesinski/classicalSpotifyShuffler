using System;
using ApplicationResources.ApplicationUtils;
using ApplicationResources.Logging;
using ApplicationResources.Services;
using ApplicationResources.Setup;
using BlazorApplicationResources.Services;
using Blazored.LocalStorage;
using ClassicalSpotifyShuffler.Utils;
using CustomResources.Utils.Extensions;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorApplicationResources.BlazorApplicationUtils
{
	public static class BlazorAppUtils
	{
		public static async Task StartApp(Func<WebAssemblyHostBuilder, Task> hostSetup, Func<Task> postSetup, ProgramUtils.StartupArgs startupArgs)
		{
			var hostBuilder = WebAssemblyHostBuilder.CreateDefault(startupArgs.CommandLineArgs);

			hostBuilder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(hostBuilder.HostEnvironment.BaseAddress) });
			hostBuilder.Services.AddBlazoredLocalStorage();
			hostBuilder.Services.AddSingleton<IDataStoreAccessor, LocalStorageAccessor>();
			hostBuilder.Services.AddSingleton<IUserInterface, AlertWindowUserInterface>();

			await hostSetup(hostBuilder).WithoutContextCapture();
			var host = hostBuilder.Build();

			await ProgramUtils.ExecuteProgram(async () =>
			{
				var consoleLogLevel = Settings.Get<LogLevel>(BasicSettings.ConsoleLogLevel);
				LoggerTargetProvider.OnLog += (args) =>
				{
					if (args.Level >= consoleLogLevel) Console.WriteLine(args.BareMessage);
				};

				LoggerTargetProvider.OnLog += (args) =>
				{
					if (args.Level == LogLevel.Error) GlobalDependencies.Get<IUserInterface>().NotifyUserOfError(args.BareMessage);
				};

				LoggerTargetProvider.Register(new LocalStorageLogger($"log.{DateTime.Now}.log"));

				Logger.Information($"Base address is {hostBuilder.HostEnvironment.BaseAddress}");

				try
				{
					await postSetup().WithoutContextCapture();
				}
				catch (Exception e)
				{
					Logger.Error("An exception occurred during initialization: {e}", e);
				}
				Logger.Information("Starting");
				await host.RunAsync();
			}, startupArgs with {
				AdditionalXmlSettingsFiles = startupArgs.AdditionalXmlSettingsFiles.Append(Utils.BlazorApplicationConstants.StandardSettingsFile)
			}, () => GlobalDependencies.InitializeWith(host.Services)).WithoutContextCapture();
		}
	}
}