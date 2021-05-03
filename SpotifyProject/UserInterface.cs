using System;
using System.Threading.Tasks;

namespace SpotifyProject
{
	public abstract class UserInterface
	{
		public static readonly UserInterface Default = new ConsoleUserInterface();
		public static UserInterface Instance = Default;

		public async virtual Task<string> ReadNextUserInputAsync() => await Task.Run(ReadNextUserInput);
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
		public override string ReadNextUserInput()
		{
			return Console.ReadLine();
		}
		public override void NotifyUser(string notification)
		{
			Console.WriteLine(notification);
		}
		public override void NotifyUserOfError(string error)
		{
			Console.Error.WriteLine(error);
		}
	}
}


