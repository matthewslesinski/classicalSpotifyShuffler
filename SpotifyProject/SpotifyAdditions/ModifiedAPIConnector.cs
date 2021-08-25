using System;
using System.Threading;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;
using SpotifyProject.Utils.Extensions;

namespace SpotifyProject.SpotifyAdditions
{
	public class ModifiedAPIConnector : ModifiedAPIConnectorBase
	{
		private static int _requestsSent = 0;

		private readonly IHTTPClient _httpClient;

		private readonly IRetryHandler _retryHandler;

		private readonly IHTTPLogger _httpLogger;

		public override event EventHandler<IResponse> ResponseReceived;

		public ModifiedAPIConnector(Uri baseAddress, IAuthenticator authenticator)
			: this(baseAddress, authenticator, new NewtonsoftJSONSerializer(), new NetHttpClient(), null, null)
		{
		}

		public ModifiedAPIConnector(Uri baseAddress, IAuthenticator authenticator, IJSONSerializer jsonSerializer, IHTTPClient httpClient, IRetryHandler retryHandler, IHTTPLogger httpLogger)
			: base(baseAddress, authenticator, jsonSerializer)
		{
			_httpClient = httpClient;
			_retryHandler = retryHandler;
			_httpLogger = httpLogger;
		}

		public override void SetRequestTimeout(TimeSpan timeout)
		{
			_httpClient.SetRequestTimeout(timeout);
		}

		protected override async Task<IResponse> DoRequest(IRequest request)
		{
			var requestNumber = Interlocked.Increment(ref _requestsSent);
			using (Loggers.HTTPLogger.BeginScope($"Request ID: {requestNumber}")) {
				IRequest request2 = request;
				IResponse response = await SendRequest(request2);
				if (_retryHandler != null)
					response = await _retryHandler!.HandleRetry(request2, response, SendRequest).WithoutContextCapture();
				
				ProcessErrors(response);
				return response;
			}
		}
		private async Task<IResponse> SendRequest(IRequest request) {
			await ApplyAuthenticator(request).WithoutContextCapture();
			_httpLogger?.OnRequest(request);
			IResponse response = await _httpClient.DoRequest(request).WithoutContextCapture();
			_httpLogger?.OnResponse(response);
			this.ResponseReceived?.Invoke(this, response);
			return response;
		}
	}
}
