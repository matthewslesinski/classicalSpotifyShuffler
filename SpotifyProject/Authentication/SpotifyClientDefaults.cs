using System;
using CustomResources.Utils.GeneralUtils;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;
using SpotifyProject.SpotifyAdditions;

namespace SpotifyProject.Authentication
{
	public class SpotifyDefaults
	{
		public readonly static RetryHandlers RetryHandlers = new RetryHandlers();
		public readonly static HttpClients HTTPClients = new HttpClients();
		public readonly static Paginators Paginators = new Paginators();
		public readonly static HTTPLoggers HTTPLoggers = new HTTPLoggers();
		public readonly static APIConnectors APIConnectors = new APIConnectors();
	}

	public class RetryHandlers
	{
		public RetryHandlers()
		{
			SimpleRetryHandler = new SimpleRetryHandler();
			ProtectedSimpleRetryHandler = new ProtectedSimpleRetryHandler();
		}

		public IRetryHandler SimpleRetryHandler { get; }
		public IRetryHandler ProtectedSimpleRetryHandler { get; }
	}

	public class Paginators
	{
		public Paginators()
		{
			SimplePaginator = new SimplePaginator();
			ConcurrentPaginator = new ConcurrentPaginator(SimplePaginator);
		}

		public IPaginator SimplePaginator { get; }
		public IPaginator ConcurrentPaginator { get; }
	}

	public class HTTPLoggers
	{
		public HTTPLoggers()
		{
			InternalLoggingWrapper = new HTTPLogger();
		}

		public ITruncatedHTTPLogger InternalLoggingWrapper { get; }
	}

	public class HttpClients
	{
		public delegate IHTTPClient HttpClientConstructor(IRetryHandler retryHandler);

		public HttpClients()
		{
			NetHttpClient = _ => new NetHttpClient();
			RetryProtectedHttpClient = retryHandler => retryHandler is ProtectedSimpleRetryHandler checkedHandler
				? new RetryProtectedHttpClient(NetHttpClient(checkedHandler), checkedHandler)
				: Exceptions.Throw<IHTTPClient>("Only an internal retry handler should be used with the internal http client");
		}

		public HttpClientConstructor NetHttpClient { get; }
		public HttpClientConstructor RetryProtectedHttpClient { get; }
	}

	public class APIConnectors
	{		
		public APIConnectors()
		{
			SimpleAPIConnector = (baseAddress, authenticator, jsonSerializer, httpClient, retryHandler, httpLogger) => 
				new APIConnector(baseAddress, authenticator, jsonSerializer, httpClient, retryHandler, httpLogger);
			ModifiedAPIConnector = (baseAddress, authenticator, jsonSerializer, httpClient, retryHandler, httpLogger) => 
				new ModifiedAPIConnector(baseAddress, authenticator, jsonSerializer, httpClient, retryHandler, httpLogger);
		}

		public APIConnectorConstructor SimpleAPIConnector { get; }
		public APIConnectorConstructor ModifiedAPIConnector { get; }
	}
}
