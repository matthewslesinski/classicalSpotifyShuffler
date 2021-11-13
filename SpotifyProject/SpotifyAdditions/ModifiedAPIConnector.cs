using System;
using System.Threading;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;
using CustomResources.Utils.Extensions;
using ApplicationResources.Logging;
using System.Net.Http;
using System.IO;

namespace SpotifyProject.SpotifyAdditions
{
	public class ModifiedAPIConnector : APIConnector
	{
		private static int _requestsSent = 0;

		public ModifiedAPIConnector(Uri baseAddress, IAuthenticator authenticator)
			: base(baseAddress, authenticator)
		{
		}

		public ModifiedAPIConnector(Uri baseAddress, IAuthenticator authenticator, IJSONSerializer jsonSerializer, IHTTPClient httpClient, IRetryHandler retryHandler, IHTTPLogger httpLogger)
			: base(baseAddress, authenticator, jsonSerializer, httpClient, retryHandler, httpLogger)
		{ }

		protected override Task<IResponse> DoRequest(IRequest request, CancellationToken? cancellationToken = null)
		{
			var requestNumber = Interlocked.Increment(ref _requestsSent);
			using (Loggers.HTTPLogger.BeginScope($"Request ID: {requestNumber}"))
			{
				try
				{
					return base.DoRequest(request, cancellationToken);
				}
				catch (HttpRequestException e) when (e.InnerException is IOException ioE && ioE.Message.Contains("Received an unexpected EOF or 0 bytes from the transport stream."))
				{
					throw new APIException("The Spotify server did not send a response", e);
				}
			}
		}
	}
}
