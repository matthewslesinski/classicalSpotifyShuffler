using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;
using SpotifyProject.Utils;

namespace SpotifyProject.SpotifyAdditions
{
	public abstract class ModifiedAPIConnectorBase : IAPIConnector
	{
		private readonly Uri _baseAddress;

		private readonly IAuthenticator _authenticator;

		private readonly IJSONSerializer _jsonSerializer;

		public abstract event EventHandler<IResponse> ResponseReceived;

		protected ModifiedAPIConnectorBase(Uri baseAddress, IAuthenticator authenticator, IJSONSerializer jsonSerializer)
		{
			_baseAddress = baseAddress;
			_authenticator = authenticator;
			_jsonSerializer = jsonSerializer;
		}

		public Task<T> Delete<T>(Uri uri)
		{
			Ensure.ArgumentNotNull(uri, "uri");
			return SendAPIRequest<T>(uri, HttpMethod.Delete);
		}

		public Task<T> Delete<T>(Uri uri, IDictionary<string, string>? parameters)
		{
			Ensure.ArgumentNotNull(uri, "uri");
			return SendAPIRequest<T>(uri, HttpMethod.Delete, parameters);
		}

		public Task<T> Delete<T>(Uri uri, IDictionary<string, string>? parameters, object? body)
		{
			Ensure.ArgumentNotNull(uri, "uri");
			return SendAPIRequest<T>(uri, HttpMethod.Delete, parameters, body);
		}

		public async Task<HttpStatusCode> Delete(Uri uri, IDictionary<string, string>? parameters, object? body)
		{
			Ensure.ArgumentNotNull(uri, "uri");
			return (await SendAPIRequestDetailed(uri, HttpMethod.Delete, parameters, body).ConfigureAwait(continueOnCapturedContext: false)).StatusCode;
		}

		public Task<T> Get<T>(Uri uri)
		{
			Ensure.ArgumentNotNull(uri, "uri");
			return SendAPIRequest<T>(uri, HttpMethod.Get);
		}

		public Task<T> Get<T>(Uri uri, IDictionary<string, string>? parameters)
		{
			Ensure.ArgumentNotNull(uri, "uri");
			return SendAPIRequest<T>(uri, HttpMethod.Get, parameters);
		}

		public async Task<HttpStatusCode> Get(Uri uri, IDictionary<string, string>? parameters, object? body)
		{
			Ensure.ArgumentNotNull(uri, "uri");
			return (await SendAPIRequestDetailed(uri, HttpMethod.Get, parameters, body).ConfigureAwait(continueOnCapturedContext: false)).StatusCode;
		}

		public Task<T> Post<T>(Uri uri)
		{
			Ensure.ArgumentNotNull(uri, "uri");
			return SendAPIRequest<T>(uri, HttpMethod.Post);
		}

		public Task<T> Post<T>(Uri uri, IDictionary<string, string>? parameters)
		{
			Ensure.ArgumentNotNull(uri, "uri");
			return SendAPIRequest<T>(uri, HttpMethod.Post, parameters);
		}

		public Task<T> Post<T>(Uri uri, IDictionary<string, string>? parameters, object? body)
		{
			Ensure.ArgumentNotNull(uri, "uri");
			return SendAPIRequest<T>(uri, HttpMethod.Post, parameters, body);
		}

		public Task<T> Post<T>(Uri uri, IDictionary<string, string>? parameters, object? body, Dictionary<string, string>? headers)
		{
			Ensure.ArgumentNotNull(uri, "uri");
			return SendAPIRequest<T>(uri, HttpMethod.Post, parameters, body, headers);
		}

		public async Task<HttpStatusCode> Post(Uri uri, IDictionary<string, string>? parameters, object? body)
		{
			Ensure.ArgumentNotNull(uri, "uri");
			return (await SendAPIRequestDetailed(uri, HttpMethod.Post, parameters, body).ConfigureAwait(continueOnCapturedContext: false)).StatusCode;
		}

		public Task<T> Put<T>(Uri uri)
		{
			Ensure.ArgumentNotNull(uri, "uri");
			return SendAPIRequest<T>(uri, HttpMethod.Put);
		}

		public Task<T> Put<T>(Uri uri, IDictionary<string, string>? parameters)
		{
			Ensure.ArgumentNotNull(uri, "uri");
			return SendAPIRequest<T>(uri, HttpMethod.Put, parameters);
		}

		public Task<T> Put<T>(Uri uri, IDictionary<string, string>? parameters, object? body)
		{
			Ensure.ArgumentNotNull(uri, "uri");
			return SendAPIRequest<T>(uri, HttpMethod.Put, parameters, body);
		}

		public async Task<HttpStatusCode> Put(Uri uri, IDictionary<string, string>? parameters, object? body)
		{
			Ensure.ArgumentNotNull(uri, "uri");
			return (await SendAPIRequestDetailed(uri, HttpMethod.Put, parameters, body).ConfigureAwait(continueOnCapturedContext: false)).StatusCode;
		}

		public async Task<HttpStatusCode> PutRaw(Uri uri, IDictionary<string, string>? parameters, object? body)
		{
			Ensure.ArgumentNotNull(uri, "uri");
			return (await SendRawRequest(uri, HttpMethod.Put, parameters, body).ConfigureAwait(continueOnCapturedContext: false)).StatusCode;
		}

		public Task<IResponse> SendRawRequest(Uri uri, HttpMethod method, IDictionary<string, string>? parameters = null, object? body = null, IDictionary<string, string>? headers = null)
		{
			IRequest request = CreateRequest(uri, method, parameters, body, headers);
			return DoRequest(request);
		}

		public async Task<T> SendAPIRequest<T>(Uri uri, HttpMethod method, IDictionary<string, string>? parameters = null, object? body = null, IDictionary<string, string>? headers = null)
		{
			IRequest request = CreateRequest(uri, method, parameters, body, headers);
			return (await DoSerializedRequest<T>(request).WithoutContextCapture()).Body;
		}

		public async Task<IResponse> SendAPIRequestDetailed(Uri uri, HttpMethod method, IDictionary<string, string>? parameters = null, object? body = null, IDictionary<string, string>? headers = null)
		{
			IRequest request = CreateRequest(uri, method, parameters, body, headers);
			return (await DoSerializedRequest<object>(request).WithoutContextCapture()).Response;
		}

		private IRequest CreateRequest(Uri uri, HttpMethod method, IDictionary<string, string>? parameters, object? body, IDictionary<string, string>? headers)
		{
			Ensure.ArgumentNotNull(uri, "uri");
			Ensure.ArgumentNotNull(method, "method");
			return new Request(_baseAddress, uri, method, headers ?? new Dictionary<string, string>(), parameters ?? new Dictionary<string, string>())
			{
				Body = body
			};
		}

		private async Task<IAPIResponse<T>> DoSerializedRequest<T>(IRequest request)
		{
			_jsonSerializer.SerializeRequest(request);
			IResponse response = await DoRequest(request).WithoutContextCapture();
			return _jsonSerializer.DeserializeResponse<T>(response);
		}

		protected async Task ApplyAuthenticator(IRequest request)
		{
			if ((_authenticator != null && !request.Endpoint.IsAbsoluteUri) || request.Endpoint.AbsoluteUri.Contains("https://api.spotify.com", StringComparison.InvariantCulture))
			{
				await _authenticator!.Apply(request, this).WithoutContextCapture();
			}
		}

		protected static void ProcessErrors(IResponse response)
		{
			Ensure.ArgumentNotNull(response, "response");
			if (response.StatusCode >= HttpStatusCode.OK && response.StatusCode < HttpStatusCode.BadRequest)
			{
				return;
			}
			throw response.StatusCode switch
			{
				HttpStatusCode.Unauthorized => new APIUnauthorizedException(response), 
				HttpStatusCode.TooManyRequests => new APITooManyRequestsException(response), 
				_ => new APIException(response), 
			};
		}

		public abstract void SetRequestTimeout(TimeSpan timeout);

		protected abstract Task<IResponse> DoRequest(IRequest request);
	}
}
