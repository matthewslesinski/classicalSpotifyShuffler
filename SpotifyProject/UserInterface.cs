using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpotifyProject.Setup;
using SpotifyProject.Utils.Extensions;

namespace SpotifyProject
{
	public abstract class UserInterface
	{
		public static readonly UserInterface Default = new ConsoleUserInterface();
		public static UserInterface Instance = Default;

		public async virtual Task<string> ReadNextUserInputAsync() => await Task.Run(ReadNextUserInput).WithoutContextCapture();
		public async Task<string> RequestResponseAsync(string requestNotification)
		{
			NotifyUser(requestNotification);
			return await ReadNextUserInputAsync().WithoutContextCapture();
		}
		public abstract string ReadNextUserInput();
		public abstract void NotifyUser(string notification);
		public abstract void NotifyUserOfError(string error);

		public virtual bool? ParseAffirmation(string response)
		{
			bool IsAffirmative(string answer) => _affirmativeResponse.StartsWith(answer.ToLower());
			bool IsNegative(string answer) => _negativeResponse.StartsWith(answer.ToLower());
			if (IsAffirmative(response))
				return true;
			if (IsNegative(response))
				return false;
			return null;
		}

		public virtual bool ShouldProceed(string questionToAskUser)
		{
			NotifyUser(questionToAskUser);
			string response;
			while((response = ReadNextUserInput()) != null)
			{
				var affirmation = ParseAffirmation(response);
				if (affirmation.HasValue)
					return affirmation.Value;
				NotifyUser($"The supplied response was not an option. Say either \"{_affirmativeResponse}\" or \"{_negativeResponse}\"");
			}
			return false;
		}


		private const string _affirmativeResponse = "yes";
		private const string _negativeResponse = "no";

	}

	public class ConsoleUserInterface : UserInterface
	{
		private readonly ConcurrentQueue<string> _presuppliedInput
			= new ConcurrentQueue<string>(Settings.Get<IEnumerable<string>>(SettingsName.SupplyUserInput) ?? Array.Empty<string>());

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


