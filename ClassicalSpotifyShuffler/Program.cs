using Microsoft.AspNetCore.Components.Web;
using ClassicalSpotifyShuffler;
using BlazorApplicationResources.BlazorApplicationUtils;
using ApplicationResources.Services;
using CustomResources.Utils.Extensions;
using SpotifyProject.Configuration;
using SpotifyProject.SpotifyUtils;

await BlazorAppUtils.StartApp(
	builder =>
	{
		builder.RootComponents.Add<App>("#app");
		builder.RootComponents.Add<HeadOutlet>("head::after");
		return Task.CompletedTask;
	},
	async () =>
	{
		var weatherForecast = await GlobalDependencies.GlobalDependencyContainer.GetRequiredService<HttpClient>().GetStringAsync("sample-data/weather.json");
		Console.WriteLine($"{weatherForecast}");
	},
	new(args)
	{
		SettingsTypes = new[] { typeof(SpotifySettings) },
		ParameterTypes = new[] { typeof(SpotifyParameters) },
		AdditionalXmlSettingsFiles = new[] { GeneralConstants.StandardSpotifySettingsFile }
	});


