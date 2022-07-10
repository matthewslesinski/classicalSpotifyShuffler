using System;
using ApplicationResources.Services;
using CustomResources.Utils.Concepts;
using CustomResources.Utils.Extensions;
using Microsoft.AspNetCore.Components;
using SpotifyProject.Authentication;

namespace ClassicalSpotifyShuffler.Implementations
{
	public static class BlazorAuthentication
	{
		internal static Task<Result<string>> RequestLoginFromUser(Uri loginUri, CancellationToken cancellationToken = default)
		{
			var navManager = GlobalDependencies.GlobalDependencyContainer.GetRequiredService<NavigationManager>();
			var uriBuilder = new UriBuilder(navManager.Uri);
			var q = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
			var code = q["code"];
			if (code != null)
				return Task.FromResult<Result<string>>(new(code));
			navManager.NavigateTo(loginUri.ToString(), false);
			// Should not be reached
			return Task.FromResult(Result<string>.None);
		}
	}


	public class BlazorSpotifyAuthCodeAuthenticator : SpotifyAuthCodeAuthenticator
	{
		public BlazorSpotifyAuthCodeAuthenticator()
		{ }

		protected override Task<Result<string>> RequestLoginFromUser(Uri loginUri, CancellationToken cancellationToken = default) =>
			BlazorAuthentication.RequestLoginFromUser(loginUri, cancellationToken);
	}

	public class BlazorSpotifyPKCEAuthenticator : SpotifyPKCEAuthenticator
	{
		public BlazorSpotifyPKCEAuthenticator()
		{ }

		protected override Task<Result<string>> RequestLoginFromUser(Uri loginUri, CancellationToken cancellationToken = default) =>
			BlazorAuthentication.RequestLoginFromUser(loginUri, cancellationToken);
	}
}