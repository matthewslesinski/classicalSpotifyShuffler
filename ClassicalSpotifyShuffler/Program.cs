using Microsoft.AspNetCore.Components.Web;
using ClassicalSpotifyShuffler;
using BlazorApplicationResources.BlazorApplicationUtils;
using ApplicationResources.Services;
using SpotifyProject.Configuration;
using SpotifyProject.SpotifyUtils;
using SpotifyProject.Authentication;
using System.Net;
using ApplicationResources.Setup;

await BlazorAppUtils.StartApp(
	builder =>
	{
		builder.RootComponents.Add<App>("#app");
		builder.RootComponents.Add<HeadOutlet>("head::after");
		builder.Services.AddImplementationForMultipleGlobalServices<SpotifyCommandLineAccountAuthenticator>(typeof(ISpotifyAuthenticator), typeof(ISpotifyAccountAuthenticator));
		builder.Services.AddSingleton<ISpotifyService>(services => new StandardSpotifyProvider(services.GetSpotifyAuthCodeAuthenticator()));
		return Task.CompletedTask;
	},
	() =>
	{
		ServicePointManager.DefaultConnectionLimit = Settings.Get<int>(SpotifySettings.NumHTTPConnections);
		return Task.CompletedTask;
	},
	new(args)
	{
		SettingsTypes = new[] { typeof(SpotifySettings) },
		ParameterTypes = new[] { typeof(SpotifyParameters) },
		AdditionalXmlSettingsFiles = new[] { GeneralConstants.StandardSpotifySettingsFile }
	});