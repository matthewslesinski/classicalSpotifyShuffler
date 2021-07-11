using System;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;
using SpotifyProject.SpotifyAdditions;

namespace SpotifyProject.Authentication
{
	public class SpotifyDefaults
	{
		public readonly static RetryHandlers RetryHandlers = new RetryHandlers();
		public readonly static Paginators Paginators = new Paginators();
		public readonly static HTTPLoggers HTTPLoggers = new HTTPLoggers();
	}

	public class RetryHandlers
	{
		public RetryHandlers()
		{
			SimpleRetryHandler = new SimpleRetryHandler();
		}

		public IRetryHandler SimpleRetryHandler { get; }
	}

	public class Paginators
	{
		public Paginators()
		{
			SimplePaginator = new SimplePaginator();
			ConcurrentObservablePaginator = new ConcurrentPaginatorWithObservables(SimplePaginator);
			ConcurrentEnumerablePaginator = new ConcurrentPaginatorWithEnumerables(SimplePaginator);
		}

		public IPaginator SimplePaginator { get; }
		public IPaginator ConcurrentObservablePaginator { get; }
		public IPaginator ConcurrentEnumerablePaginator { get; }
	}

	public class HTTPLoggers
	{
		public HTTPLoggers()
		{
			InternalLoggingWrapper = new HTTPLogger();
		}

		public ITruncatedHTTPLogger InternalLoggingWrapper;
	}
}
