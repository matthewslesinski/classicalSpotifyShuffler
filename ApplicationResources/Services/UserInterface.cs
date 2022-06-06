using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApplicationResources.Logging;
using ApplicationResources.Setup;
using CustomResources.Utils.Extensions;

namespace ApplicationResources.Services
{
	public interface IUserInterface
	{
		Task<string> RequestResponseAsync(params string[] requestNotifications);
		void NotifyUser(string alertMessage);
		void NotifyUserOfError(string error);

		public bool? ParseConfirmation(string response)
		{
			bool IsAffirmative(string answer) => _affirmativeResponse.StartsWith(answer.ToLower());
			bool IsNegative(string answer) => _negativeResponse.StartsWith(answer.ToLower());
			if (IsAffirmative(response))
				return true;
			if (IsNegative(response))
				return false;
			return null;
		}

		public async Task<bool> ShouldProceed(string questionToAskUser)
		{
			var response = await RequestResponseAsync(questionToAskUser).WithoutContextCapture();
			while (response != null)
			{
				var confirmation = ParseConfirmation(response);
				if (confirmation.HasValue)
					return confirmation.Value;
				response = await RequestResponseAsync($"The supplied response was not an option. Say either \"{_affirmativeResponse}\" or \"{_negativeResponse}\"").WithoutContextCapture();
			}
			return false;
		}


		private const string _affirmativeResponse = "yes";
		private const string _negativeResponse = "no";
	}

	public abstract class UserInterface : IUserInterface
	{
		public async virtual Task<string> ReadNextUserInputAsync() => await Task.Run(ReadNextUserInput).WithoutContextCapture();
		public Task<string> RequestResponseAsync(params string[] requestNotifications)
		{
			requestNotifications.Each(NotifyUser);
			return ReadNextUserInputAsync();
		}
		public abstract string ReadNextUserInput();
		public abstract void NotifyUser(string notification);
		public abstract void NotifyUserOfError(string error);
	}

	public class ConsoleUserInterface : UserInterface
	{
		private readonly ConcurrentQueue<string> _presuppliedInput
			= new ConcurrentQueue<string>(Settings.Get<IEnumerable<string>>(BasicSettings.SupplyUserInput) ?? Array.Empty<string>());

		public override string ReadNextUserInput()
		{
			if (_presuppliedInput.TryDequeue(out var input))
			{
				NotifyUser($"Using input supplied ahead of time: \"{input}\"");
			}
			else
				input = Console.ReadLine();

			Logger.Verbose($"Received the following input from the user via the command line: \"{input}\"");
			return input;
		}
		public override void NotifyUser(string notification)
		{
			Console.WriteLine(notification);
			Logger.Verbose($"Notifying the user via the command line with this message: \"{notification}\"");
		}
		public override void NotifyUserOfError(string error)
		{
			Console.Error.WriteLine(error);
			Logger.Verbose($"Notifying the user via the command line of the following error: \"{error}\"");
		}
	}
}


