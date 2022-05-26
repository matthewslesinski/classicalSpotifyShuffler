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

	//The following is just a implementation to reproduce the vtable setup error in Mono.
	//TODO Once this error is fixed, remove the three last lines in the first property group in SpotifyProjectCommandLine.csproj

	//public interface I1<T>
	//{
	//	void Foo();
	//}

	//public interface I2<T, T2> : I1<T>
	//{
	//	void I1<T>.Foo()
	//	{
	//		Console.WriteLine("Error not reproduced");
	//	}
	//}

	//public class C : I2<int, int>
	//{
	//}

	//public class Program
	//{
	//	public static void Main()
	//	{
	//		var isMono = typeof(object).Assembly.GetType("Mono.RuntimeStructs") != null;
	//		Console.WriteLine($"isMono = {isMono}");

	//		I1<int> v = new C();
	//		v.Foo();
	//	}
	//}
}
