using System;
using System.Collections.Generic;
using System.Threading;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;
using SpotifyProject.Utils;

namespace SpotifyProject.SpotifyAdditions
{
	public class ConcurrentPaginatorWithEnumerables : ConcurrentPaginator
	{
		public ConcurrentPaginatorWithEnumerables(IPaginator fallBackPaginator) : base(fallBackPaginator)
		{
		}

		protected override IAsyncEnumerable<T> GetPages<T>(IAPIConnector connector, IEnumerable<Uri> uris, CancellationToken cancel)
		{
			return uris.RunInParallel(uri => connector.Get<T>(uri).ConfigureAwait(false), cancel);
		}
	}
}
