using Microsoft.AspNetCore.Components.Web;
using ClassicalSpotifyShuffler;
using BlazorApplicationResources.BlazorApplicationUtils;
using ApplicationResources.Services;
using SpotifyProject.Configuration;
using SpotifyProject.SpotifyUtils;
using ApplicationResources.Logging;

await BlazorAppUtils.StartApp(
	builder =>
	{
		builder.RootComponents.Add<App>("#app");
		builder.RootComponents.Add<HeadOutlet>("head::after");
		return Task.CompletedTask;
	},
	async () =>
	{
		LoggerTargetProvider.OnLog += (logArgs) => Console.WriteLine(logArgs.FullMessage);
		var weatherForecast = await GlobalDependencies.Get<HttpClient>().GetStringAsync("sample-data/weather.json");
		Logger.Information($"{weatherForecast}");
	},
	new(args)
	{
		SettingsTypes = new[] { typeof(SpotifySettings) },
		ParameterTypes = new[] { typeof(SpotifyParameters) },
		AdditionalXmlSettingsFiles = new[] { GeneralConstants.StandardSpotifySettingsFile }
	});