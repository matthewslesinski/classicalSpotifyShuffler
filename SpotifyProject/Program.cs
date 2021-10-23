using System;
using System.Threading.Tasks;
using System.Threading;
using SpotifyProject.Authentication;
using ApplicationResources.Setup;
using CustomResources.Utils.Extensions;
using ApplicationResources.ApplicationUtils;
using ApplicationResources.Logging;
using SpotifyProject.SpotifyPlaybackModifier;
using SpotifyProject.Configuration;

namespace SpotifyProject
{
    class Program
    {
        static void Main(string[] args)
        {
            ProgramUtils.ExecuteProgram(args, () =>
            {
                var task = Run();
                while (!task.IsCompleted)
                {
                    Thread.Sleep(Timeout.Infinite);
                }
                // Unreachable on purpose in case the compiler would want to get rid of the preceding while loop
                Console.WriteLine("Terminating successfully");
                Environment.Exit(0);
            }, ApplicationConstants.XmlSettingsFileFlag, typeof(SpotifySettings));
		}

        static async Task Run()
        {
            try
            {
                Logger.Information("Starting Spotify Project");
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
