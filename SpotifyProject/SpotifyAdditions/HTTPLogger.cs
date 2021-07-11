using System;
using System.Linq;
using System.Threading;
using SpotifyAPI.Web.Http;
using SpotifyProject.Utils;

namespace SpotifyProject.SpotifyAdditions
{
    public interface ITruncatedHTTPLogger : IHTTPLogger
	{ 
        public int? CharacterLimit { set; }
	}

    /**
     * Can be passed to SpotifyAPI to route that library's internal logging to the Logger class
     */
    public class HTTPLogger : ITruncatedHTTPLogger
    {
        private const string OnRequestFormat = "Sending Spotify Request: {0} {1} [{2}] {3}";
        private const string OnResponseFormat = "Received Spotify Response: {0} {1} {2}";

        public HTTPLogger()
        {
        }

		public int? CharacterLimit { private get; set; }

		public void OnRequest(IRequest request)
        {
            string parameters = null;
            if (request.Parameters != null)
            {
                parameters = string.Join(",",
                    request.Parameters.Select(kv => kv.Key + "=" + kv.Value)?.ToArray() ?? Array.Empty<string>()
                );
            }

            Loggers.HTTPLogger.Verbose(string.Format(OnRequestFormat, request.Method, request.Endpoint, parameters, request.Body).Truncate(CharacterLimit));
        }

        public void OnResponse(IResponse response)
        {
            var body = response.Body?.ToString()?.Replace("\n", "", StringComparison.InvariantCulture);
            Loggers.HTTPLogger.Verbose(string.Format(OnResponseFormat, response.StatusCode, response.ContentType, body).Truncate(CharacterLimit));
        }
    }
}
