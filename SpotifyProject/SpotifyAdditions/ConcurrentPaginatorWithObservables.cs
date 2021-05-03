using System;
using SpotifyAPI.Web;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Joins;
using System.Reactive.Threading;
using System.Reactive.Subjects;
using System.Reactive.Disposables;
using System.Linq;
using System.Collections.Generic;
using SpotifyAPI.Web.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SpotifyProject.SpotifyAdditions
{
	public class ConcurrentPaginatorWithObservables : ConcurrentPaginator
	{
		public ConcurrentPaginatorWithObservables(IPaginator fallbackPaginator) : base(fallbackPaginator)
		{
		}

		protected override IAsyncEnumerable<T> GetPages<T>(IAPIConnector connector, IEnumerable<Uri> uris, CancellationToken cancel = default)
		{
            var rest = uris.ToObservable().Select(uri => Observable.FromAsync(async () => await connector.Get<T>(uri))).Merge(Environment.ProcessorCount * 2);
            return rest.ToAsyncEnumerable();
        }
    }
}
