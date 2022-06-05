using System;
using ApplicationResources.ApplicationUtils;
using ApplicationResources.Logging;
using ApplicationResources.Services;
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
				Console.WriteLine("Starting");
				Logger.Information("Starting");

				Console.WriteLine($"Base address is {hostBuilder.HostEnvironment.BaseAddress}");
				await postSetup().WithoutContextCapture();
				await host.RunAsync();
			}, startupArgs with {
				AdditionalXmlSettingsFiles = startupArgs.AdditionalXmlSettingsFiles.Append(BlazorApplicationResources.Utils.BlazorApplicationConstants.StandardSettingsFile)
			}, () => GlobalDependencies.InitializeWith(host.Services)).WithoutContextCapture();
		}
	}
}