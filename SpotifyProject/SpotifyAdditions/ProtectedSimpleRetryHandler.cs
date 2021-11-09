using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using ApplicationResources.ApplicationUtils.Parameters;
using ApplicationResources.Logging;
using CustomResources.Utils.GeneralUtils;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;
using SpotifyProject.Configuration;
using SpotifyProject.Utils;

namespace SpotifyProject.SpotifyAdditions
{
	// Based off of the SimpleRetryHandlerClass from SpotifyAPI.Web, but with some changes. The SpotifyAPI.Web implementation does not expose
	// anything that can be overridden, so in order to emulate most of the functionality, a whole new class with very similar functionality had to be created.
	public class ProtectedSimpleRetryHandler : IRetryHandler
	{
		private static readonly IReadOnlyDictionary<TimeSpan, (TimeSpan escalateAfter, TimeSpan deEscalateAfter)> _escalationConfig =
			new Dictionary<TimeSpan, (TimeSpan escalateAfter, TimeSpan deEscalateAfter)>
		{
				{ TimeSpan.FromSeconds(1), (TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(120)) },
				{ TimeSpan.FromSeconds(2), (TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60)) },
				{ TimeSpan.FromSeconds(5), (TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30)) },
				{ TimeSpan.FromSeconds(15), (TimeSpan.FromSeconds(120), TimeSpan.FromSeconds(30)) },
				{ TimeSpan.FromSeconds(30), (TimeSpan.MaxValue, TimeSpan.FromSeconds(30)) },
		};

		private static readonly IReadOnlyDictionary<TimeSpan, TimeSpan> _nextEscalations = new Dictionary<TimeSpan, TimeSpan>
		{
			{ TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2) },
			{ TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5) },
			{ TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15) },
			{ TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30) },
		};

		private readonly Func<TimeSpan, Task> _sleep;

		public TimeSpan RetryAfter { get; set; }

		public static int RetryTimes => TaskParameters.Get<int>(SpotifyParameters.NumberOfRetriesForServerError);

		public bool TooManyRequestsConsumesARetry { get; set; }

		public IReadOnlySet<HttpStatusCode> RetryErrorCodes { get; set; }

		public bool MakeRetryAfterGuessIfNecessary { get; set; }

		private readonly ConcurrentStack<(TimeSpan retryWaitTime, DateTime escalateAfter, DateTime deEscalateAfter)> _fallbackWaitTimes =
			new ConcurrentStack<(TimeSpan retryWaitTime, DateTime escalateAfter, DateTime deEscalateAfter)>();

		public ProtectedSimpleRetryHandler()
			: this(new Func<TimeSpan, Task>(Task.Delay))
		{
		}

		public ProtectedSimpleRetryHandler(Func<TimeSpan, Task> sleep)
		{
			_sleep = sleep;
			RetryAfter = TimeSpan.FromMilliseconds(50.0);
			TooManyRequestsConsumesARetry = false;
			MakeRetryAfterGuessIfNecessary = true;
			RetryErrorCodes = SpotifyConstants.NonDeterministicStatusCodes;
		}

		private TimeSpan? ParseTooManyRetries(IResponse response)
		{
			if (response.StatusCode != HttpStatusCode.TooManyRequests)
			{
				return null;
			}
			if ((response.Headers.ContainsKey("Retry-After") && int.TryParse(response.Headers["Retry-After"], out var result)) || (response.Headers.ContainsKey("retry-after") && int.TryParse(response.Headers["retry-after"], out result)))
			{
				return TimeSpan.FromSeconds(result);
			}
			if (!MakeRetryAfterGuessIfNecessary)
			{
				throw new APIException("429 received, but unable to parse Retry-After Header. This should not happen!");
			} else
			{
				Logger.Error($"The Spotify Api returned a 429 without a Retry-After Header. This should not happen, but it did. We are making a guess for how long to wait");
				// TODO: This is pretty hacky, but at the time of writing it is not that important
				while (_fallbackWaitTimes.TryPeek(out var waitTimeInfo) && waitTimeInfo.deEscalateAfter < DateTime.Now)
					_fallbackWaitTimes.TryPop(out _);
				(TimeSpan waitTime, DateTime escalationTime, DateTime deEscalationTime) currentWaitTimeInfo;
				while (!_fallbackWaitTimes.TryPeek(out currentWaitTimeInfo))
				{
					var waitTime = TimeSpan.FromSeconds(1);
					var (escalationTime, deEscalationTime) = _escalationConfig[waitTime];
					var whenToEscalate = DateTime.Now + escalationTime;
					var whenToDeEscalate = DateTime.Now + deEscalationTime;
					_fallbackWaitTimes.Push((waitTime, whenToEscalate, whenToDeEscalate));
				}
				if (currentWaitTimeInfo.escalationTime < DateTime.Now)
				{
					var nextWaitTime = _nextEscalations[currentWaitTimeInfo.waitTime];
					var (escalationTime, deEscalationTime) = _escalationConfig[nextWaitTime];
					var whenToEscalate = DateTime.Now + escalationTime;
					var whenToDeEscalate = DateTime.Now + deEscalationTime;
					_fallbackWaitTimes.Push((nextWaitTime, whenToEscalate, whenToDeEscalate));
					return nextWaitTime;
				}
				else
					return currentWaitTimeInfo.waitTime;
			}
		}

		public Task<IResponse> HandleRetry(IRequest request, IResponse response, IRetryHandler.RetryFunc retry)
		{
			Ensure.ArgumentNotNull(response, "response");
			Ensure.ArgumentNotNull(retry, "retry");
			return HandleRetryInternally(request, response, retry, RetryTimes);
		}

		private async Task<IResponse> HandleRetryInternally(IRequest request, IResponse response, IRetryHandler.RetryFunc retry, int triesLeft)
		{
			TimeSpan? timeSpan = ParseTooManyRetries(response);
			if (timeSpan.HasValue && (!TooManyRequestsConsumesARetry || triesLeft > 0))
			{
				Logger.Information("Received a 429 (Too Many Retries) from the Spotify API. Waiting {}ms before trying again", timeSpan.Value.TotalMilliseconds);
				await _sleep(timeSpan.Value).ConfigureAwait(continueOnCapturedContext: false);
				response = await retry(request).ConfigureAwait(continueOnCapturedContext: false);
				int triesLeft2 = TooManyRequestsConsumesARetry ? (triesLeft - 1) : triesLeft;
				return await HandleRetryInternally(request, response, retry, triesLeft2).ConfigureAwait(continueOnCapturedContext: false);
			}
			if (RetryErrorCodes.Contains(response.StatusCode) && triesLeft > 0)
			{
				Logger.Information("Received a server error from the Spotify API. Retrying again, with {numberOfRetries} retries left", triesLeft);
				await _sleep(RetryAfter).ConfigureAwait(continueOnCapturedContext: false);
				response = await retry(request).ConfigureAwait(continueOnCapturedContext: false);
				return await HandleRetryInternally(request, response, retry, triesLeft - 1).ConfigureAwait(continueOnCapturedContext: false);
			}
			return response;
		}
	}
}
