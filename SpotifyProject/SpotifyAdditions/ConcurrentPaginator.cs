using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;
using System.Linq;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;

namespace SpotifyProject.SpotifyAdditions
{
    public class ConcurrentPaginator : IPaginator
    {
        protected readonly IPaginator _fallbackPaginator;

        public ConcurrentPaginator(IPaginator fallbackPaginator)
		{
            _fallbackPaginator = fallbackPaginator;
		}

        public async Task<IList<T>> PaginateAll<T>(IPaginatable<T> firstPage, IAPIConnector connector, CancellationToken? cancellationToken = null)
        {
			Ensure.ArgumentNotNull(firstPage, nameof(firstPage));
			Ensure.ArgumentNotNull(connector, nameof(connector));

			return firstPage is Paging<T> firstPaging && TryPaginateConcurrently<T, Paging<T>>(firstPaging, firstPaging.Items, page => page?.Items, connector, cancellationToken, out var resultTasks)
				? await resultTasks.MakeAsync(cancellationToken ?? default).ToListAsync().WithoutContextCapture()
				: await _fallbackPaginator.PaginateAll(firstPage, connector, cancellationToken).WithoutContextCapture();
        }

        public async Task<IList<T>> PaginateAll<T, TNext>(IPaginatable<T, TNext> firstPage, Func<TNext, IPaginatable<T, TNext>> mapper, IAPIConnector connector, CancellationToken? cancellationToken = null)
		{
			Ensure.ArgumentNotNull(firstPage, nameof(firstPage));
			Ensure.ArgumentNotNull(mapper, nameof(mapper));
			Ensure.ArgumentNotNull(connector, nameof(connector));

			return firstPage is Paging<T> firstPaging && TryPaginateConcurrently<T, TNext>(firstPaging, firstPaging.Items, tnext => mapper(tnext)?.Items, connector, cancellationToken, out var resultTasks)
				? await resultTasks.MakeAsync(cancellationToken ?? default).ToListAsync().WithoutContextCapture()
				: await _fallbackPaginator.PaginateAll(firstPage, mapper, connector, cancellationToken).WithoutContextCapture();
		}

        public IAsyncEnumerable<T> Paginate<T>(IPaginatable<T> firstPage, IAPIConnector connector, CancellationToken cancel = default)
        {
			Ensure.ArgumentNotNull(firstPage, nameof(firstPage));
			Ensure.ArgumentNotNull(connector, nameof(connector));

			return firstPage is Paging<T> firstPaging && TryPaginateConcurrently<T, Paging<T>>(firstPaging, firstPaging.Items, page => page?.Items, connector, cancel, out var resultTasks)
				? resultTasks.MakeAsync(cancel)
				: _fallbackPaginator.Paginate(firstPage, connector, cancel);
		}

		public IAsyncEnumerable<T> Paginate<T, TNext>(IPaginatable<T, TNext> firstPage, Func<TNext, IPaginatable<T, TNext>> mapper, IAPIConnector connector, CancellationToken cancel = default)
        {
			Ensure.ArgumentNotNull(firstPage, nameof(firstPage));
			Ensure.ArgumentNotNull(mapper, nameof(mapper));
			Ensure.ArgumentNotNull(connector, nameof(connector));

			return firstPage is Paging<T, TNext> firstPaging && TryPaginateConcurrently<T, TNext>(firstPaging, firstPaging.Items, tnext => mapper(tnext)?.Items, connector, cancel, out var resultTasks)
				? resultTasks.MakeAsync(cancel)
				: _fallbackPaginator.Paginate(firstPage, mapper, connector, cancel);
        }

		public IEnumerable<Task<T>> PaginateConcurrently<T, TPaginatable>(TPaginatable firstPage, IAPIConnector connector, CancellationToken? cancellationToken = null)
		    where TPaginatable : IPaginatable<T>, IFinitePaginatable
		{
			Ensure.ArgumentNotNull(firstPage, nameof(firstPage));
			Ensure.ArgumentNotNull(connector, nameof(connector));

			return TryPaginateConcurrently<T, TPaginatable>(firstPage, firstPage.Items, page => page?.Items, connector, cancellationToken, out var results)
				? results
				: _fallbackPaginator.PaginateConcurrently<T, TPaginatable>(firstPage, connector, cancellationToken);
		}

		public IEnumerable<Task<T>> PaginateConcurrently<T, TNext, TPaginatable>(TPaginatable firstPage, Func<TNext, TPaginatable> mapper,
		    IAPIConnector connector, CancellationToken? cancellationToken = null)
		    where TPaginatable : IPaginatable<T, TNext>, IFinitePaginatable
		{
			Ensure.ArgumentNotNull(firstPage, nameof(firstPage));
			Ensure.ArgumentNotNull(mapper, nameof(mapper));
			Ensure.ArgumentNotNull(connector, nameof(connector));

			return TryPaginateConcurrently<T, TNext>(firstPage, firstPage.Items, tnext => mapper(tnext)?.Items, connector, cancellationToken, out var results)
				? results
				: _fallbackPaginator.PaginateConcurrently<T, TNext, TPaginatable>(firstPage, mapper, connector, cancellationToken);
		}

		#region implementation

		private static bool TryPaginateConcurrently<TItem, TRequestResult>(IFinitePaginatable firstPage, IEnumerable<TItem> firstPageItems,
			Func<TRequestResult, List<TItem>> itemsMapper, IAPIConnector connector, CancellationToken? cancellationToken, out IEnumerable<Task<TItem>> results)
		{
			if (firstPageItems == null)
				throw new ArgumentException("The first page has to contain an Items list!", nameof(firstPageItems));

			if (!TryGetAllOtherUris(firstPage, out var uriContainers))
			{
				results = Array.Empty<Task<TItem>>();
				return false;
			}

			var otherItems = RequestOtherPageContentsConcurrently(uriContainers, itemsMapper, connector, cancellationToken);
			results = firstPageItems.Select(item => Task.FromResult(item)).Concat(otherItems);
			return true;
		}

		private static bool TryGetAllOtherUris(IFinitePaginatable firstPage, out IEnumerable<PageUriContainer> uris) =>
			TryGetAllOtherUris(firstPage.Next, firstPage.Total, firstPage.Offset, firstPage.Limit, out uris);

		private static bool TryGetAllOtherUris(string nextUri, int? totalItems, int? firstOffset, int? firstLimit, out IEnumerable<PageUriContainer> uris)
		{
			uris = Array.Empty<PageUriContainer>();
			if (nextUri == null)
				return true;

			if (!totalItems.HasValue || !firstOffset.HasValue || !firstLimit.HasValue)
				return false;

			IEnumerable<PageUriContainer> GetAllNextUrisWhenSafe()
			{
				var parts = _offsetRegex.Split(nextUri);
				for (int startOffset = firstOffset.Value + firstLimit.Value; startOffset < totalItems.Value; startOffset += firstLimit.Value)
				{
					var newNextUri = parts[0] + startOffset + parts[1];
					var endExclusive = Math.Min(startOffset + firstLimit.Value, totalItems.Value);
					yield return new PageUriContainer { Uri = new Uri(newNextUri, UriKind.Absolute), StartOffset = startOffset, Count = endExclusive - startOffset };
				}
			}
			uris = GetAllNextUrisWhenSafe();
			return true;
		}

		private static IEnumerable<Task<TResult>> RequestOtherPageContentsConcurrently<TResult, TIntermediate>(IEnumerable<PageUriContainer> uriContainers,
			Func<TIntermediate, List<TResult>> itemsMapper, IAPIConnector connector, CancellationToken? cancellationToken)
		{
			return uriContainers
				.Select(uriInfo => (uriInfo, connector.Get<TIntermediate>(uriInfo.Uri, cancellationToken)))
				.SelectMany(pair =>
				{
					var uriInfo = pair.uriInfo;
					var request = pair.Item2;
					return Enumerable.Range(0, uriInfo.Count).Select(async index =>
					{
						var results = itemsMapper(await request.ConfigureAwait(false));
						if (results == null)
							throw new InvalidOperationException("The server returned an invalid response with no items");
						return results[index];
					});
				}).ToList();
		}

		private static readonly Regex _offsetRegex = new("(?<=offset=)\\d+");

		protected struct PageUriContainer
		{
			public Uri Uri { get; set; }
			public int StartOffset { get; set; }
			public int Count { get; set; }
		}

		#endregion
    }
}