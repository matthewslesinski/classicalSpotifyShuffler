using System;
using System.Linq;
using System.Threading.Tasks;
using ApplicationResources.ApplicationUtils.Parameters;
using ApplicationResources.Logging;
using CustomResources.Utils.Extensions;
using NUnit.Framework;
using SpotifyAPI.Web;
using SpotifyProject.Authentication;
using SpotifyProject.Configuration;
using SpotifyProject.SpotifyPlaybackModifier;

namespace SpotifyProjectTests.SpotifyApiTests
{
	public class SpotifyApiExperiments : SpotifyTestBase
	{

		[Test]
		public async Task MeasureRateLimit()
		{
			using (TaskParameters.GetBuilder()
				.With(SpotifyParameters.HTTPClientName, nameof(SpotifyDefaults.HTTPClients.NetHttpClient))
				.With(SpotifyParameters.RetryHandlerName, nameof(SpotifyDefaults.RetryHandlers.NullRetryHandler))
				.With(SpotifyParameters.HTTPLoggerCharacterLimit, 50)
				.Apply())
			{

				var client = await Authenticators.Authenticate(Authenticators.AuthorizationCodeAuthenticator);
				var tempAccessor = new SpotifyAccessorBase(client);
				var albumId = SampleAlbumIds[SampleAlbums.BachKeyboardWorks];
				async Task<(bool, TimeSpan?)> SendRequest()
				{
					try
					{
						await tempAccessor.GetAlbum(albumId);
					}
					catch (APITooManyRequestsException e)
					{
						return (true, e.RetryAfter);
					}
					return (false, null);
				}
				var arr = new int[5];
				for (int i = 0; i < 5; i++)
				{
					var count = 1;
					(bool, TimeSpan?) result;
					while (!(result = await SendRequest().WithoutContextCapture()).Item1)
						count += 1;
					arr[i] = count;
					Logger.Information($"Hit rate limit after {count} attempts, with retry after timespan {result.Item2}");
					Assert.IsNotNull(result.Item2);
					await Task.Delay(result.Item2.Value).WithoutContextCapture();
				}
				Logger.Information($"API Rate Limit Measurements: {arr}");
				Assert.Pass();
			}

		}
	}
}
