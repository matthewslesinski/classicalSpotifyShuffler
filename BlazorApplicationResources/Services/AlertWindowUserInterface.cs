using System;
using ApplicationResources.Logging;
using ApplicationResources.Services;
using Microsoft.JSInterop;
using CustomResources.Utils.Extensions;

namespace BlazorApplicationResources.Services
{
	public class AlertWindowUserInterface : IUserInterface
	{
		private IJSRuntime Javascript => GlobalDependencies.Get<IJSRuntime>();
		public void NotifyUser(string alertMessage)
		{
			Logger.Information("Notifying user of message {alertMessage}", alertMessage);
			_ = Javascript.InvokeVoidAsync("alert", alertMessage).AsTask()
					.WrapInErrorHandler<Exception>(e => Logger.Error("Failed to notify user of message \"{alertMessage}\" because of error: {error}", alertMessage, e));
		}

		public void NotifyUserOfError(string error)
		{
			NotifyUser($"An unexpected error has occurred:\n{error}.\nThe console may have more details.");
		}

		public async Task<string> RequestResponseAsync(params string[] requestNotifications)
		{
			var message = string.Join('\n', requestNotifications);
			string response = await Javascript.InvokeAsync<string>("prompt", message);
			return response;
		}

		async Task<bool> IUserInterface.ShouldProceed(string questionToAskUser)
		{
			return await Javascript.InvokeAsync<bool>("confirm", questionToAskUser);
		}

	}
}