using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using SpotifyAPI.Web.Http;
using SpotifyProject.Utils;
using SpotifyProject.Utils.Extensions;

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
        private const string _multipleSpacesRegexString = "\\s+";
        private const string _onRequestFormat = "Sending Spotify Request: {0} {1} [{2}] {3}";
        private const string _onResponseFormat = "Received Spotify Response: {0} {1} {2}";

        private readonly static Regex _multipleSpacesRegex = new Regex(_multipleSpacesRegexString);

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

            Loggers.HTTPLogger.Verbose(string.Format(_onRequestFormat, request.Method, request.Endpoint, parameters, request.Body).Trim().Truncate(CharacterLimit));
        }

        public void OnResponse(IResponse response)
        {
            
            var body = response.Body?.ToString()?.Replace(_multipleSpacesRegex, " ");
            Loggers.HTTPLogger.Verbose(string.Format(_onResponseFormat, response.StatusCode, response.ContentType, body).Trim().Truncate(CharacterLimit));
        }
    }
}
