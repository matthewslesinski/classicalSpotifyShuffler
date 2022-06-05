using System;
using ApplicationResources.ApplicationUtils;
using ApplicationResources.Logging;
using ApplicationResources.Services;
using ApplicationResources.Setup;
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

			await hostSetup(hostBuilder).WithoutContextCapture();
			var host = hostBuilder.Build();

			await ProgramUtils.ExecuteProgram(async () =>
			{
				var consoleLogLevel = Settings.Get<LogLevel>(BasicSettings.ConsoleLogLevel);
				LoggerTargetProvider.OnLog += (args) =>
				{
					if (args.Level >= consoleLogLevel) Console.WriteLine(args.BareMessage);
				};

				Logger.Information("Starting");
				Logger.Information($"Base address is {hostBuilder.HostEnvironment.BaseAddress}");

				await postSetup().WithoutContextCapture();
				await host.RunAsync();
			}, startupArgs with {
				AdditionalXmlSettingsFiles = startupArgs.AdditionalXmlSettingsFiles.Append(Utils.BlazorApplicationConstants.StandardSettingsFile)
			}, () => GlobalDependencies.InitializeWith(host.Services)).WithoutContextCapture();
		}
	}
}