using System;
using ApplicationResources.Services;
using CustomResources.Utils.Concepts;
using Microsoft.AspNetCore.Components;
using SpotifyProject.Authentication;

namespace ClassicalSpotifyShuffler.Implementations
{
	public class BlazorSpotifyAccountAuthenticator : SpotifyAccountAuthenticator
	{
		public BlazorSpotifyAccountAuthenticator()
		{ }

		protected override Task<Result<string>> RequestLoginFromUser(Uri loginUri, CancellationToken cancellationToken = default)
		{
			var navManager = GlobalDependencies.GlobalDependencyContainer.GetRequiredService<NavigationManager>();
			var uriBuilder = new UriBuilder(navManager.Uri);
			var q = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
			var code = q["code"];
			if (code != null)
				return Task.FromResult<Result<string>>(new(code));
			navManager.NavigateTo(loginUri.ToString(), false);
			return Task.FromResult(Result<string>.None);
		}
	}
}