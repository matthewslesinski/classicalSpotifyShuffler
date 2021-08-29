using System;
using SpotifyAPI.Web;
using System.Reactive.Linq;
using System.Linq;
using System.Collections.Generic;
using SpotifyAPI.Web.Http;
using System.Threading;

namespace SpotifyProject.SpotifyAdditions
{
	public class ConcurrentPaginatorWithObservables : ConcurrentPaginator
	{
		public ConcurrentPaginatorWithObservables(IPaginator fallbackPaginator) : base(fallbackPaginator)
		{
		}

		protected override IAsyncEnumerable<T> GetPages<T>(IAPIConnector connector, IEnumerable<Uri> uris, CancellationToken cancel = default)
		{
            var rest = uris.ToObservable().Select(uri => Observable.FromAsync(() => connector.Get<T>(uri))).Merge(Environment.ProcessorCount * 2);
            return rest.ToAsyncEnumerable();
        }
    }
}
