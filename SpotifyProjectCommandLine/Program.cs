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
				AdditionalXmlSettingsFiles = new[] { GeneralConstants.StandardSpotifySettingsFile }
			}, () => GlobalDependencies.Initialize(args)
						.AddGlobalService<IDataStoreAccessor, FileAccessor>()
						.Build());
		}

		static async Task Run()
		{
			try
			{
				Logger.Information("Starting Spotify Project");
				var authorizationSettingsPath = Path.Combine(Settings.Get<string>(BasicSettings.ProjectRootDirectory), GeneralConstants.SuggestedAuthorizationSettingsFile);
				await Settings.RegisterProvider(new XmlSettingsProvider(authorizationSettingsPath)).WithoutContextCapture();
				var spotify = await Authenticators.Authenticate(Authenticators.AuthorizationCodeAuthenticator).WithoutContextCapture();
				var reorderer = new SpotifyPlaybackReorderer(spotify);
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