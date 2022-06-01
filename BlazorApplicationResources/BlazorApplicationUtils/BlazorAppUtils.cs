using System;
using ApplicationResources.ApplicationUtils;
using ApplicationResources.Logging;
using ApplicationResources.Services;
using Blazored.LocalStorage;
using ClassicalSpotifyShuffler.Utils;
using CustomResources.Utils.Extensions;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using static ApplicationResources.ApplicationUtils.ProgramUtils;

namespace BlazorApplicationResources.BlazorApplicationUtils
{
	public static class BlazorAppUtils
	{
		public static async Task StartApp(Func<WebAssemblyHostBuilder, Task> hostSetup, Func<Task> postSetup, StartupArgs startupArgs, Func<IDisposable>? dependencyInitializer = null)
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
			}, startupArgs, () => GlobalDependencies.InitializeWith(host.Services)).WithoutContextCapture();
		}
	}
}