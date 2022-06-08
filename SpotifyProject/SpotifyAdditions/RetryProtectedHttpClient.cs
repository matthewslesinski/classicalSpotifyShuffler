using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ApplicationResources.ApplicationUtils;
using ApplicationResources.Logging;
using ApplicationResources.Setup;
using CustomResources.Utils.Concepts;
using CustomResources.Utils.Concepts.DataStructures;
using CustomResources.Utils.Extensions;
using SpotifyAPI.Web.Http;
using SpotifyProject.Configuration;

namespace SpotifyProject.SpotifyAdditions
{
	public class RetryProtectedHttpClient : StandardDisposable, IHTTPClient
	{
		private readonly IHTTPClient _underlyingClient;
		private readonly TaskQueue<IRequest, IResponse> _cautionQueue;
		private readonly IRequestTracker<IRequest> _requestTracker;
		private readonly ProtectedSimpleRetryHandler _retryHandler;
		private readonly IConcurrentDictionary<long, IRequestTrackerNode> _returnedResponseNodes;

		public RetryProtectedHttpClient(IHTTPClient underlyingClient, ProtectedSimpleRetryHandler retryHandler, CancellationToken cancellationToken = default)
		{
			_returnedResponseNodes = new InternalConcurrentDictionary<long, IRequestTrackerNode>();
			_retryHandler = retryHandler;
			retryHandler.OnResponseParsedForRetries += MarkAsFinishedExternal;
			_underlyingClient = underlyingClient;
			_cautionQueue = new CallbackTaskQueue<IRequest, IResponse>(SendRequest, cancellationToken);
			_requestTracker = new RequestTracker();
		}

		public void SetRequestTimeout(TimeSpan timeout)
		{
			_underlyingClient.SetRequestTimeout(timeout);
		}

		public Task<IResponse> DoRequest(IRequest request, CancellationToken? cancel = null)
		{
			if (_alreadyDisposed == 1)
				throw new InvalidOperationException("The client has already been disposed");
			switch (_requestTracker.CheckCautionLevel(out _)) 
			{
				case CautionLevel.Reckless:
					Logger.Verbose("Sending http request immediately");
					return SendRequest(request, cancel ?? default);
				case CautionLevel.Cautious:
				case CautionLevel.Waiting:
					Logger.Verbose("Queued http request for sending");
					return _cautionQueue.Schedule(request, cancel ?? default);
				default:
					throw new NotImplementedException();
			};
		}

		private async Task<IResponse> SendRequest(IRequest request, CancellationToken cancellationToken)
		{
			var added = false;
			IRequestTrackerNode requestNode = null;
			while (!added)
			{
				while (_requestTracker.CheckCautionLevel(out var waitAtLeast) == CautionLevel.Waiting)
				{
					await Task.Delay(waitAtLeast, cancellationToken);
					if (_alreadyDisposed == 1)
						throw new OperationCanceledException($"The request has been cancelled due to the HTTP client being disposed: {request}");
				}
				cancellationToken.ThrowIfCancellationRequested();

				added = _requestTracker.TryTrackRequest(request, DateTime.Now, out requestNode);
			}
			var requestResult = RequestResult.FailedNotSent;

			IResponse response;
			try
			{
				Logger.Verbose("Sending request to Spotify API");
				var sendTask = _underlyingClient.DoRequest(request, cancellationToken);
				requestResult = RequestResult.FailedSent;
				response = new ResponseWrapper(requestNode.Id, await sendTask.WithoutContextCapture());
			}
			catch
			{
				_requestTracker.MarkAsFinished(requestNode, requestResult, DateTime.Now);
				throw;
			}
			_returnedResponseNodes.Add(requestNode.Id, requestNode);
			return response;
		}

		private void MarkAsFinishedExternal(IResponse response, TimeSpan? timestamp)
		{
			if (response is not ResponseWrapper internalResponse) 
				throw new ArgumentException($"Only an internally returned response can be marked as finished. Received {response}");
			if (_returnedResponseNodes.TryRemove(internalResponse.Id, out var requestNode))
				_requestTracker.MarkAsFinished(requestNode, timestamp.HasValue ? RequestResult.CompletedHitRateLimit : RequestResult.CompletedSuccessfully, DateTime.Now, timestamp);
			else
				Logger.Warning("Can't mark the already finished response with ID {id} as finished", internalResponse.Id);
		}

		protected override void DoDispose()
		{
			_underlyingClient.Dispose();
			_cautionQueue.Dispose();
			_requestTracker.Dispose();
			_retryHandler.OnResponseParsedForRetries -= MarkAsFinishedExternal;
		}
	}

	internal enum CautionLevel
	{
		Reckless,
		Cautious,
		Waiting
	}

	internal enum RequestResult
	{
		CompletedSuccessfully,
		CompletedHitRateLimit,
		FailedSent,
		FailedNotSent,
		FailedUnknown,
		InProgress
	}

	internal interface IRequestTracker<RequestT> : IDisposable
	{
		bool TryTrackRequest(RequestT request, DateTime? sendTime, out IRequestTrackerNode addedNode);
		void MarkAsFinished(IRequestTrackerNode requestNode, RequestResult requestResult, DateTime? finishedTime, TimeSpan? retryAfter = null);
		CautionLevel CheckCautionLevel(out TimeSpan waitAtLeast);
	}

	internal interface IRequestTracker<NodeT, RequestT> : IRequestTracker<RequestT> where NodeT : IRequestTrackerNode
	{
		bool IRequestTracker<RequestT>.TryTrackRequest(RequestT request, DateTime? sendTime, out IRequestTrackerNode addedObject)
		{
			var success = TryTrackRequest(request, sendTime, out NodeT addedNode);
			addedObject = addedNode;
			return success;
		}
		bool TryTrackRequest(RequestT request, DateTime? sendTime, out NodeT addedNode);
		void IRequestTracker<RequestT>.MarkAsFinished(IRequestTrackerNode requestNode, RequestResult requestResult, DateTime? finishedTime, TimeSpan? retryAfter)
		{
			if (requestNode is NodeT requestIdCasted) MarkAsFinished(requestIdCasted, requestResult, finishedTime, retryAfter);
			else throw new ArgumentException("The request Node is not valid", nameof(requestNode));
		}
		void MarkAsFinished(NodeT requestNode, RequestResult requestResult, DateTime? finishedTime, TimeSpan? retryAfter = null);
	}

	internal interface IRequestTrackerNode
	{
		long Id { get; }
	}

	internal interface IRequestStatsTracker : IDisposable
	{
		int NumOutForCaution { get; }
		int NumOutForHold { get; }
		void Record(DateTime startTime, DateTime endTime, RequestResult result, int numOut) => Record(new StatsData(startTime, endTime, result, numOut));
		void Record(StatsData statsUpdate);

		internal record StatsData(DateTime StartTime, DateTime EndTime, RequestResult Result, int NumOut);
	}

	internal class RequestTracker : StandardDisposable, IRequestTracker<RequestTracker.Node, IRequest>
	{
		private readonly IRequestTrackerQueue _queue;
		private readonly IRequestStatsTracker _stats;
		private long _nodeId = 0;
		private int _numOut = 0;
		private long _apiResponseHoldUntil = DateTime.MinValue.Ticks;
		private DateTime APIResponseHoldUntil { get => new DateTime(Interlocked.Read(ref _apiResponseHoldUntil)); set => Interlocked.Exchange(ref _apiResponseHoldUntil, value.Ticks); }

		internal RequestTracker()
		{
			_queue = new BagBasedRequestTrackerQueue();
			_stats = new NaiveStatsTracker();
		}

		public bool TryTrackRequest(IRequest request, DateTime? sendTime, out Node addedNode)
		{
			while (true)
			{
				Purge();
				var limit = _stats.NumOutForHold;
				var currentNumOut = _numOut;
				if (APIResponseHoldUntil > DateTime.Now || currentNumOut >= limit)
				{
					addedNode = default;
					return false;
				}
				var resultingNumOut = currentNumOut + 1;
				if (Interlocked.CompareExchange(ref _numOut, resultingNumOut, currentNumOut) == currentNumOut)
				{
					addedNode = new Node(Interlocked.Increment(ref _nodeId), request, RequestResult.InProgress, sendTime ?? DateTime.Now, resultingNumOut);
					return true;
				}
			}
		}

		public void MarkAsFinished(Node requestNode, RequestResult requestResult, DateTime? finishedTime, TimeSpan? retryAfter = null)
		{
			if (requestResult == RequestResult.InProgress)
				throw new ArgumentException("Can't mark a request finished with result \"In Progress\"");

			if (requestNode.Status != RequestResult.InProgress)
				Logger.Warning("Attempting to mark an already completed request as finished, request: {}", requestNode.Request);
			requestNode.Status = requestResult;

			if ((requestResult == RequestResult.CompletedHitRateLimit) != (retryAfter != null))
				throw new ArgumentException($"The given request result, {requestResult}, should have a retry after if and only if it is {RequestResult.CompletedHitRateLimit}");

			var endTime = finishedTime ?? DateTime.Now;
			if (retryAfter.HasValue)
				APIResponseHoldUntil = endTime + retryAfter.Value;
			if (requestResult == RequestResult.FailedNotSent)
				Interlocked.Decrement(ref _numOut);
			else
			{
				_queue.Enqueue(requestNode, endTime);
				try
				{
					_stats.Record(requestNode.SendTime, endTime, requestResult, requestNode.NumOutAfterSent);
				}
				catch (Exception e)
				{
					Logger.Error("An error occurred while recording statistics for spotify requests: {exception}", e);
				}
			}
		}

		public CautionLevel CheckCautionLevel(out TimeSpan waitAtLeast)
		{
			while (true) {
				Purge();
				var holdUntil = APIResponseHoldUntil;
				if (holdUntil > DateTime.Now)
				{
					waitAtLeast = holdUntil - DateTime.Now;
					return CautionLevel.Waiting;
				}
				var numOut = _numOut;
				var numOutForHold = _stats.NumOutForHold;
				if (numOut < numOutForHold)
				{
					waitAtLeast = default;
					return numOut < _stats.NumOutForCaution ? CautionLevel.Reckless : CautionLevel.Cautious;
				}
				if ((waitAtLeast = _queue.MinTimeStamp - DateTime.Now) >= TimeSpan.Zero)
					return CautionLevel.Waiting;
			}
		}

		private void Purge()
		{
			DateTime holdUntil;
			while ((holdUntil = APIResponseHoldUntil) <= DateTime.Now && holdUntil != DateTime.MinValue)
			{
				Interlocked.CompareExchange(ref _apiResponseHoldUntil, DateTime.MinValue.Ticks, holdUntil.Ticks);
			}
			var apiRateLimitWindow = Settings.Get<TimeSpan>(SpotifySettings.APIRateLimitWindow);
			while (_queue.RemoveEarliestBefore(DateTime.Now - apiRateLimitWindow))
			{
				Interlocked.Decrement(ref _numOut);
			}
		}

		protected override void DoDispose()
		{
			_queue.Dispose();
			_stats.Dispose();
		}

		internal record Node(long Id, IRequest Request, RequestResult Status, DateTime SendTime, int NumOutAfterSent) : IRequestTrackerNode
		{
			public RequestResult Status { get; set; } = Status;
		}

		internal interface IRequestTrackerQueue : IDisposable
		{
			void Enqueue(Node requestNode, DateTime receivedTime);
			bool RemoveEarliestBefore(DateTime timestamp);
			DateTime MinTimeStamp { get; }
		}
	}

	internal class BagBasedRequestTrackerQueue : Flusher<QueueContent, Bag>, RequestTracker.IRequestTrackerQueue
	{
		private readonly InternalConcurrentSinglyLinkedQueue<QueueContent> _orderedOldTimes = new InternalConcurrentSinglyLinkedQueue<QueueContent>();
		private Bag _prevBag = new();


		internal BagBasedRequestTrackerQueue(TimeSpan? bagCollectionWaitTime = null)
			: base(bagCollectionWaitTime ?? (Settings.Get<TimeSpan>(SpotifySettings.APIRateLimitWindow) / 6), false)
		{ }

		public DateTime MinTimeStamp
		{
			get
			{
				var queueMin = _orderedOldTimes.TryPeek(out var foundMin) ? foundMin.ResponseReceivedTime : DateTime.MaxValue;
				var bagMin = _currentFlushableContainer.MinSeen;
				var oldBagMin = _prevBag.MinSeen;
				if (queueMin == DateTime.MaxValue && bagMin == DateTime.MaxValue && oldBagMin == DateTime.MaxValue)
					throw new InvalidOperationException("Cannot request the minimum timestamp in an empty queue");
				var minTicks = Math.Min(queueMin.Ticks, Math.Min(bagMin.Ticks, oldBagMin.Ticks));
				return new DateTime(minTicks);
			}
		}

		public void Enqueue(RequestTracker.Node requestNode, DateTime responseReceivedTime) => Add(new QueueContent(requestNode, responseReceivedTime));

		public bool RemoveEarliestBefore(DateTime timestamp) => _orderedOldTimes.TryDequeueIf(queueContent => queueContent.ResponseReceivedTime <= timestamp, out _);

		protected override Task<AdditionalFlushOptions> Flush(Bag containerToFlush)
		{
			var newBufferBag = containerToFlush;

			var maxToAdd = _prevBag?.Max()?.ResponseReceivedTime ?? DateTime.MinValue;
			var splitElements = newBufferBag.ToLookup(element => element.ResponseReceivedTime <= maxToAdd);
			var olderElements = splitElements.TryGetValues(true, out var foundOldElements) ? foundOldElements : Array.Empty<QueueContent>();
			var recentElements = splitElements.TryGetValues(false, out var foundRecentElements) ? foundRecentElements : Array.Empty<QueueContent>();

			var bagToCollect = Interlocked.Exchange(ref _prevBag, new Bag(recentElements)).As<IEnumerable<QueueContent>>() ?? Array.Empty<QueueContent>();
			var elementsToAddToQueue = bagToCollect.Concat(olderElements).Ordered();
			foreach (var oldElement in elementsToAddToQueue)
				_orderedOldTimes.Enqueue(oldElement);

			return Task.FromResult(recentElements.Any() ? AdditionalFlushOptions.NeedsAdditionalFlush : AdditionalFlushOptions.NoAdditionalFlushNeeded);
		}

		protected override Bag CreateNewContainer() => new Bag();
	}


	internal record QueueContent(RequestTracker.Node RequestNode, DateTime ResponseReceivedTime) : IComparable<QueueContent>
	{
		public int CompareTo(QueueContent other) => ResponseReceivedTime.CompareTo(other.ResponseReceivedTime);
	}

	internal class Bag : IEnumerable<QueueContent>, IFlushableContainer<QueueContent>
	{
		internal Bag()
		{
			_elements = new InternalConcurrentSet<QueueContent>();
		}
		internal Bag(IEnumerable<QueueContent> initialElements)
		{
			_elements = new List<QueueContent>();
			initialElements.Each(Add);
		}

		private readonly static long _startingTicks = DateTime.MaxValue.Ticks;
		private readonly ICollection<QueueContent> _elements;
		private long _minTicks = _startingTicks;

		internal int ScheduledToBeCollected = false.AsInt();

		internal DateTime MinSeen => new DateTime(Interlocked.Read(ref _minTicks));

		internal void Add(QueueContent element)
		{
			_elements.Add(element);
			var elementTicks = element.ResponseReceivedTime.Ticks;
			if (Interlocked.Read(ref _minTicks) == _startingTicks && Interlocked.CompareExchange(ref _minTicks, elementTicks, _startingTicks) == _startingTicks)
				return;

			while (true)
			{
				var currTicks = Interlocked.Read(ref _minTicks);
				if (elementTicks > currTicks || Interlocked.CompareExchange(ref _minTicks, Math.Min(currTicks, elementTicks), currTicks) == currTicks)
					break;
			}

			return;
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public IEnumerator<QueueContent> GetEnumerator() => _elements.GetEnumerator();

		public bool Update(QueueContent itemToFlush) { Add(itemToFlush); return true; }

		public bool RequestFlush() => CustomResources.Utils.GeneralUtils.Utils.IsFirstRequest(ref ScheduledToBeCollected);
	}

	internal abstract class BaseLinearStatsTracker : TaskContainingDisposable, IRequestStatsTracker
	{
		protected static readonly CalculatedStats _startingStats = new CalculatedStats(0, 0, 0, 0);
		private readonly CachedData<CalculatedStats> _statsDataStore;
		private readonly BlockingCollection<IRequestStatsTracker.StatsData> _statsUpdates;

		public BaseLinearStatsTracker()
		{
			var dataStoreFileName = Path.Combine(Settings.Get<string>(BasicSettings.ProjectRootDirectory), Settings.Get<string>(SpotifySettings.APIRateLimitStatsFile));
			_statsDataStore = new CachedJSONData<CalculatedStats>(dataStoreFileName, fileAccessType: CachedData<CalculatedStats>.FileAccessType.SlightlyLongFlushing,
				useDefaultValue: true, defaultValue: _startingStats);
			_statsUpdates = new BlockingCollection<IRequestStatsTracker.StatsData>();
			_statsDataStore.OnValueLoaded += (loadedValue) => Logger.Verbose("{statsType}: Loaded rate limit stats from {loadedStats}", GetType().Name, loadedValue);
			_statsDataStore.OnValueChanged += (oldValue, newValue) => Logger.Verbose("{statsType}: Updated rate limit stats from {oldStats} to {newStats}", GetType().Name, oldValue, newValue);
			Run(DoCalculations);
		}

		public abstract int NumOutForCaution { get; }

		public abstract int NumOutForHold { get; }

		public void Record(IRequestStatsTracker.StatsData statsUpdate) => _statsUpdates.Add(statsUpdate);

		protected abstract CalculatedStats CalculateNewStats(CalculatedStats oldStats, DateTime startTime, DateTime endTime, RequestResult result, int numOut);

		protected CalculatedStats CurrentStats { get => _statsDataStore.CachedValue ?? _startingStats; set => _statsDataStore.CachedValue = value; }

		protected override void DoDispose()
		{
			_statsUpdates.CompleteAdding();
			base.DoDispose();
			_statsUpdates.Dispose();
			_statsDataStore.Dispose();
		}

		private async Task DoCalculations()
		{
			if (!_statsDataStore.IsLoaded)
				await _statsDataStore.Initialize().WithoutContextCapture();
			foreach(var (startTime, endTime, requestResult, numOut) in _statsUpdates.GetConsumingEnumerable(StopToken))
			{
				if (!_alreadyDisposed.AsBool())
				{
					var newStats = CalculateNewStats(CurrentStats, startTime, endTime, requestResult, numOut);
					CurrentStats = newStats;
				}
			}
		}

		protected record CalculatedStats(int MaxOutWithoutHit, int MaxOutWhenHit, int CumulativeOutWhenHit, int NumHits)
		{
			public int AverageOutWhenHit => NumHits <= 0 ? 0 : CumulativeOutWhenHit / NumHits;
		}
	}

	internal class NaiveStatsTracker : BaseLinearStatsTracker
	{
		public override int NumOutForCaution
		{
			get
			{
				var currentStats = CurrentStats;
				return currentStats.NumHits <= 0
					? currentStats.MaxOutWithoutHit
					: (currentStats.AverageOutWhenHit >> 1) + (currentStats.MaxOutWithoutHit >> 2);
			}
		}

		public override int NumOutForHold => int.MaxValue;

		protected override CalculatedStats CalculateNewStats(CalculatedStats oldStats, DateTime startTime, DateTime endTime, RequestResult result, int numOut)
		{
			var wasHit = result == RequestResult.CompletedHitRateLimit;
			return wasHit
				? oldStats with { MaxOutWhenHit = Math.Max(oldStats.MaxOutWhenHit, numOut), CumulativeOutWhenHit = oldStats.CumulativeOutWhenHit + numOut, NumHits = oldStats.NumHits + 1 }
				: oldStats with { MaxOutWithoutHit = Math.Max(oldStats.MaxOutWithoutHit, numOut) };
		}
	}

	internal record ResponseWrapper(long Id, IResponse WrappedResponse) : IResponse, IWrapper<IResponse>
	{
		public IResponse WrappedObject => WrappedResponse;

		public object Body => WrappedResponse.Body;

		public IReadOnlyDictionary<string, string> Headers => WrappedResponse.Headers;

		public HttpStatusCode StatusCode => WrappedResponse.StatusCode;

		public string ContentType => WrappedResponse.ContentType;
	}
}
