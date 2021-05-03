using System;
using System.Linq;
using SpotifyAPI.Web.Http;

namespace SpotifyProject
{
    /**
     * Can be passed to SpotifyAPI to route that library's internal logging to the Logger class
     */
    public class HTTPLogger : IHTTPLogger
    {
        private const string OnRequestFormat = "Sending Spotify Request: {0} {1} [{2}] {3}";
        private const string OnResponseFormat = "Received Spotify Response: {0} {1} {2}";
        private readonly int _responseBodySizeLimit;

        public HTTPLogger(int responseBodySizeLimit = 150)
        {
            _responseBodySizeLimit = responseBodySizeLimit;
        }

        public void OnRequest(IRequest request)
        {

            string parameters = null;
            if (request.Parameters != null)
            {
                parameters = string.Join(",",
                    request.Parameters.Select(kv => kv.Key + "=" + kv.Value)?.ToArray() ?? Array.Empty<string>()
                );
            }

            Logger.Verbose(OnRequestFormat, request.Method, request.Endpoint, parameters, request.Body);
        }

        public void OnResponse(IResponse response)
        {
            var body = response.Body?.ToString()?.Replace("\n", "", StringComparison.InvariantCulture);
            if (body != null && body.Length > _responseBodySizeLimit)
                body = body?.Substring(0, Math.Min(_responseBodySizeLimit, body.Length));
            Logger.Verbose(OnResponseFormat, response.StatusCode, response.ContentType, body);
        }
    }
}
