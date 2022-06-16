using System;
using ApplicationResources.Services;
using Microsoft.Extensions.DependencyInjection;
using SpotifyProject.Authentication;

namespace SpotifyProject.SpotifyUtils
{
	public interface IGlobalSpotifyServiceUser : IGlobalServiceUser
	{
		public ISpotifyAuthenticator Authenticator => GlobalDependencies.GlobalDependencyContainer.GetSpotifyAuthenticator();
		public ISpotifyAccountAuthenticator AuthCodeAuthenticator => GlobalDependencies.GlobalDependencyContainer.GetSpotifyAuthCodeAuthenticator();
		public ISpotifyService Spotify => GlobalDependencies.GlobalDependencyContainer.GetSpotifyProvider();

	}

	public static class IGlobalSpotifyDependencyExtensions
	{
		public static ISpotifyAuthenticator GetSpotifyAuthenticator(this IServiceProvider serviceProvider) => serviceProvider.GetRequiredService<ISpotifyAuthenticator>();
		public static ISpotifyAccountAuthenticator GetSpotifyAuthCodeAuthenticator(this IServiceProvider serviceProvider) => serviceProvider.GetRequiredService<ISpotifyAccountAuthenticator>();
		public static ISpotifyService GetSpotifyProvider(this IServiceProvider serviceProvider) => serviceProvider.GetRequiredService<ISpotifyService>();

		public static ISpotifyAuthenticator AccessLocalDataStore(this IGlobalSpotifyServiceUser dependent) => dependent.Authenticator;
		public static ISpotifyAccountAuthenticator AccessUserInterface(this IGlobalSpotifyServiceUser dependent) => dependent.AuthCodeAuthenticator;
		public static ISpotifyService AccessSpotify(this IGlobalSpotifyServiceUser dependent) => dependent.Spotify;
	}
}

