using System;
using System.Threading.Tasks;
using System.Threading;
using McMaster.Extensions.CommandLineUtils;
using SpotifyAPI.Web;
using SpotifyProject.Authentication;
using SpotifyProject.Utils;
using SpotifyProject.Setup;
using SpotifyProject.SpotifyAdditions;
using System.IO;
using System.Linq;

namespace SpotifyProject
{
    class Program
    {
        private const string _xmlSettingsFileFlag = "--settingsXmlFile";

        static void Main(string[] args)
        {
			AppDomain.CurrentDomain.UnhandledException += (sender, args) => Logger.Error($"An Exception occurred: {args.ExceptionObject}");
			var app = new CommandLineApplication();
			Settings.RegisterProvider(CommandLineOptions.Initialize(app));
            var xmlSettingsFileOption = app.Option(_xmlSettingsFileFlag, "The file name for settings stored in xml format", CommandOptionType.MultipleValue);
			app.OnExecute(() =>
			{
                if (xmlSettingsFileOption.HasValue())
                    Settings.RegisterProviders(xmlSettingsFileOption.Values.Where(File.Exists).Select(fileName => new XmlSettingsProvider(fileName)));
                if (File.Exists(Constants.SuggestedAuthorizationSettingsFile))
                    Settings.RegisterProvider(new XmlSettingsProvider(Constants.SuggestedAuthorizationSettingsFile));
                if (File.Exists(Constants.StandardSettingsFile))
                    Settings.RegisterProvider(new XmlSettingsProvider(Constants.StandardSettingsFile));
                Settings.Load();
				var task = Run();
				while (!task.IsCompleted)
				{
					Thread.Sleep(Timeout.Infinite);
				}
				// Unreachable on purpose in case the compiler would want to get rid of the preceding while loop
				Console.WriteLine("Terminating successfully");
				Environment.Exit(0);
			});
			app.Execute(args);
		}

        static async Task Run()
        {
            try
            {
                Logger.Information("Starting Spotify Project");
				var spotify = await Authenticators.Authenticate(Authenticators.AuthorizationCodeAuthenticator);
				var reorderer = new SpotifyPlaybackReorderer(spotify);
                if (Settings.Get<bool>(SettingsName.AskUser))
                    await reorderer.ShuffleUserProvidedContext();
                else
				    await reorderer.ShuffleCurrentPlayback();
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
