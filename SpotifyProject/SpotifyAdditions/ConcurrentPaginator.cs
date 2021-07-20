using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;
using System.Linq;
using SpotifyProject.Utils.Extensions;

namespace SpotifyProject.SpotifyAdditions
{
    public abstract class ConcurrentPaginator : IPaginator
    {
        protected readonly IPaginator _fallbackPaginator;

        public ConcurrentPaginator(IPaginator fallbackPaginator)
		{
            _fallbackPaginator = fallbackPaginator;
		}

        public async Task<IList<T>> PaginateAll<T>(IPaginatable<T> firstPage, IAPIConnector connector)
        {
            return await Paginate(firstPage, connector).ToListAsync().WithoutContextCapture();
        }

        public async Task<IList<T>> PaginateAll<T, TNext>(IPaginatable<T, TNext> firstPage, Func<TNext, IPaginatable<T, TNext>> mapper, IAPIConnector connector)
        {
            return await Paginate(firstPage, mapper, connector).ToListAsync().WithoutContextCapture();
        }

        public IAsyncEnumerable<T> Paginate<T>(IPaginatable<T> firstPage, IAPIConnector connector, CancellationToken cancel = default)
        {
            if (firstPage.Items == null)
                throw new ArgumentException("The first page has to contain an Items list!", nameof(firstPage));

            if (!(firstPage is Paging<T> page) || !TryGetAllNextUris(page, out var nextUris))
                return _fallbackPaginator.Paginate(firstPage, connector, cancel);

            return page.Items.ToAsyncEnumerable().Concat(GetPages<Paging<T>>(connector, nextUris, cancel)
                .SelectMany(page => (page.Items ?? new List<T>()).ToAsyncEnumerable()));
        }

        public IAsyncEnumerable<T> Paginate<T, TNext>(IPaginatable<T, TNext> firstPage, Func<TNext, IPaginatable<T, TNext>> mapper, IAPIConnector connector, CancellationToken cancel = default)
        {
            if (firstPage.Items == null)
                throw new ArgumentException("The first page has to contain an Items list!", nameof(firstPage));

            if (!(firstPage is Paging<T, TNext> page) || !TryGetAllNextUris(page, out var nextUris))
                return _fallbackPaginator.Paginate(firstPage, mapper, connector, cancel);

            return page.Items.ToAsyncEnumerable().Concat(GetPages<TNext>(connector, nextUris, cancel)
                .Select(mapper)
                .SelectMany(page => (page.Items ?? new List<T>()).ToAsyncEnumerable()));
        }

        protected abstract IAsyncEnumerable<T> GetPages<T>(IAPIConnector connector, IEnumerable<Uri> uris, CancellationToken cancel);


        protected static bool TryGetAllNextUris<T>(Paging<T> firstPage, out IEnumerable<Uri> uris) =>
            TryGetAllNextUris(firstPage.Next, firstPage.Total, firstPage.Offset, firstPage.Limit, out uris);

        protected static bool TryGetAllNextUris<T, TNext>(Paging<T, TNext> firstPage, out IEnumerable<Uri> uris) =>
            TryGetAllNextUris(firstPage.Next, firstPage.Total, firstPage.Offset, firstPage.Limit, out uris);

        private static bool TryGetAllNextUris(string nextUri, int? totalItems, int? firstOffset, int? firstLimit, out IEnumerable<Uri> uris)
        {
            uris = Array.Empty<Uri>();
            if (nextUri == null)
                return true;

            if (!totalItems.HasValue || firstOffset != 0 || !firstLimit.HasValue)
            {
                Logger.Error("The paginated response Spotify returned is in an unexpected format. Not all values were provided or were as expected");
                return false;
            }

            IEnumerable<Uri> GetAllNextUrisWhenSafe()
            {
                var parts = _offsetRegex.Split(nextUri);
                for (int i = firstLimit.Value; i < totalItems.Value; i += firstLimit.Value)
                {
                    var newNextUri = parts[0] + i + parts[1];
                    yield return new Uri(newNextUri, UriKind.Absolute);
                }
            }
            uris = GetAllNextUrisWhenSafe();
            return true;
        }

        private static readonly Regex _offsetRegex = new Regex("(?<=offset=)\\d+");
    }
}