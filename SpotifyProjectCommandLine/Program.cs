using System;
using System.Threading.Tasks;
using SpotifyProject.Authentication;
using ApplicationResources.Setup;
using CustomResources.Utils.Extensions;
using ApplicationResources.ApplicationUtils;
using ApplicationResources.Logging;
using SpotifyProject.SpotifyPlaybackModifier;
using SpotifyProject.Configuration;
using SpotifyProject.SpotifyUtils;
using System.IO;
using System.Runtime.CompilerServices;
using ApplicationResources.Services;
using System.Net;

[assembly: InternalsVisibleTo("SpotifyProjectTests")]

namespace SpotifyProjectCommandLine
{
	class Program
	{
		static Task Main(string[] args)
		{
			return ProgramUtils.ExecuteCommandLineProgram(Run, new ProgramUtils.StartupArgs(args)
			{
				XmlSettingsFileFlag = ApplicationConstants.XmlSettingsFileFlag,
				SettingsTypes = new[] { typeof(SpotifySettings) },
				ParameterTypes = new[] { typeof(SpotifyParameters) },
				AdditionalXmlSettingsFiles = new[] { GeneralConstants.StandardConfigurationSettingsFile, GeneralConstants.StandardRunSettingsFile, GeneralConstants.StandardSpotifySettingsFile }
			}, () => GlobalDependencies.Initialize(args)
						.AddGlobalService<IDataStoreAccessor, FileAccessor>()
						.AddGlobalService<IUserInterface, ConsoleUserInterface>()
						.AddImplementationForMultipleGlobalServices<SpotifyCommandLineAccountAuthenticator>(typeof(ISpotifyAccountAuthenticator), typeof(ISpotifyAuthenticator))
						.AddGlobalService<ISpotifyService>(services => new StandardSpotifyProvider(services.GetSpotifyAuthCodeAuthenticator()))
						.Build());
		}

		static async Task Run()
		{
			try
			{
				Logger.Information("Starting Spotify Project");
				ServicePointManager.DefaultConnectionLimit = Settings.Get<int>(SpotifySettings.NumHTTPConnections);
				var authorizationSettingsPath = ApplicationResources.Utils.GeneralUtils.GetAbsoluteCombinedPath(
					Settings.Get<string>(BasicSettings.ProjectRootDirectory),
					Settings.Get<string>(SpotifySettings.PersonalDataDirectory),
					GeneralConstants.SuggestedAuthorizationSettingsFile);
				if (await GlobalDependencies.GlobalDependencyContainer.GetLocalDataStore().ExistsAsync(authorizationSettingsPath).WithoutContextCapture())
					await Settings.RegisterProvider(new XmlSettingsProvider(authorizationSettingsPath)).WithoutContextCapture();
				await GlobalDependencies.GlobalDependencyContainer.GetSpotifyProvider().InitializeAsync().WithoutContextCapture();
				await GlobalDependencies.GlobalDependencyContainer.GetSpotifyAuthenticator().Authenticate().WithoutContextCapture();
				var spotifyProvider = GlobalDependencies.GlobalDependencyContainer.GetSpotifyProvider();
				var reorderer = new SpotifyPlaybackReorderer(spotifyProvider.Client);
				if (Settings.Get<bool>(SpotifySettings.AskUser))
					await reorderer.ShuffleUserProvidedContext().WithoutContextCapture();
				else
					await reorderer.ShuffleCurrentPlayback().WithoutContextCapture();
				Logger.Information("Terminating successfully");
				Environment.Exit(0);
			}
			catch (Exception e)
			{
				Logger.Error($"An Exception occurred: {e}");
				Logger.Information("Terminating due to error");
				Environment.Exit(1);
			}
		}
	}
}