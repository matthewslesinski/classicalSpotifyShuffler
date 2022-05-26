using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ClassicalSpotifyShuffler;
using ApplicationResources.Services;
using ApplicationResources.ApplicationUtils;
using CustomResources.Utils.Extensions;
using SpotifyProject.Configuration;
using SpotifyProject.SpotifyUtils;
using ApplicationResources.Logging;
using static ClassicalSpotifyShuffler.Pages.FetchData;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using ClassicalSpotifyShuffler.Utils;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddSingleton<IDataStoreAccessor, LocalStorageAccessor>();

var host = builder.Build();

await ProgramUtils.ExecuteProgram(async () =>
{
	Console.WriteLine("Starting");
	Logger.Information("Starting");

	Console.WriteLine($"Base address is {builder.HostEnvironment.BaseAddress}");
	var weatherForecast = await GlobalDependencies.GlobalDependencyContainer.GetRequiredService<HttpClient>().GetStringAsync("sample-data/weather.json");
	Console.WriteLine($"{weatherForecast}");
	await host.RunAsync();
}, new(args)
{
	SettingsTypes = new[] { typeof(SpotifySettings) },
	ParameterTypes = new[] { typeof(SpotifyParameters) },
	AdditionalXmlSettingsFiles = new[] { GeneralConstants.StandardSpotifySettingsFile }
}, () => GlobalDependencies.InitializeWith(host.Services)).WithoutContextCapture();

