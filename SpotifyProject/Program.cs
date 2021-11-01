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

namespace SpotifyProject
{
    class Program
    {
        static void Main(string[] args)
        {
            ProgramUtils.ExecuteProgramAsync(Run, new ProgramUtils.StartupArgs(args)
            {
                XmlSettingsFileFlag = ApplicationConstants.XmlSettingsFileFlag,
                SettingsTypes = new [] { typeof(SpotifySettings) },
                ParameterTypes = new [] { typeof(SpotifyParameters) },
                AdditionalXmlSettingsFiles = new [] { GeneralConstants.StandardSpotifySettingsFile }
            });
		}

        static async Task Run()
        {
            try
            {
                Logger.Information("Starting Spotify Project");
                Settings.RegisterProvider(new XmlSettingsProvider(Path.Combine(Settings.Get<string>(BasicSettings.ProjectRootDirectory), GeneralConstants.SuggestedAuthorizationSettingsFile)));
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
