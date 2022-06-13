using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CustomResources.Utils.Concepts.DataStructures;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;

namespace CustomResources.Utils.Concepts.DataStructures
{
	public interface IQueryCache<T>
	{
		Task<List<T>> GetAll(CancellationToken cancellationToken = default);
		Task<List<T>> GetSubsequence(int start, int count, CancellationToken cancellationToken = default);
		Task<int> GetTotalCount(CancellationToken cancellationToken = default);
		Task SoftRefresh(CancellationToken cancellationToken = default);
		Task HardRefresh(CancellationToken cancellationToken = default);
		Task Initialize(CancellationToken cancellationToken = default);
		void Stop();
	}

	public enum LoadType
	{
		Lazy,
		OnInitialization,
		PartiallyOnInitialization
	}

	public abstract class QueryCache<T, ResponseT> : IQueryCache<T>
	{
		protected int? _knownTotal = null;
		protected readonly int _maxBatchSize;
		private readonly MutableClassReference<TaskCompletionSource<Node[]>> _initialLoadCompletionSource = new(null);
		private readonly LoadType _loadType;
		private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();

		public QueryCache(int maxBatchSize, LoadType loadType = LoadType.Lazy, int? knownTotal = null, IList<T> knownInitialValues = null)
		{
			_maxBatchSize = maxBatchSize;
			_loadType = loadType;
			if (knownTotal.HasValue)
			{
				_knownTotal = knownTotal;
				_initialLoadCompletionSource.Value = NewTaskCompletionSourceWithKnownArraySize(knownTotal.Value, knownInitialValues);
			}
		}

		public async Task<int> GetTotalCount(CancellationToken cancellationToken = default) =>
			(await GetOrCreateNodesArray(WrapCancellationToken(cancellationToken)).WithoutContextCapture()).Length;

		public Task<List<T>> GetAll(CancellationToken cancellationToken = default) => GetSubsequence(0, int.MaxValue, cancellationToken);

		public async Task<List<T>> GetSubsequence(int start, int count, CancellationToken cancellationToken = default)
		{
			if (start < 0)
				throw new ArgumentOutOfRangeException($"start: {start} was less than 0");

			if (count < 0)
				throw new ArgumentOutOfRangeException($"count: {count} was less than 0");
			cancellationToken = WrapCancellationToken(cancellationToken);
			var nodes = await GetOrCreateNodesArray(cancellationToken, start, count).WithoutContextCapture();
			cancellationToken.ThrowIfCancellationRequested();
			count = Math.Min(count, nodes.Length - start);
			return await GetSubsequenceImpl(nodes, start, count, cancellationToken).WithoutContextCapture();
		}

		public Task HardRefresh(CancellationToken cancellationToken = default)
		{
			var taskCompletionSourceToReplaceWith = _knownTotal.HasValue ? NewTaskCompletionSourceWithKnownArraySize(_knownTotal.Value) : null;
			_initialLoadCompletionSource.Change(null);

			return Task.CompletedTask;
		}

		public virtual Task SoftRefresh(CancellationToken cancellationToken = default) => HardRefresh(cancellationToken);

		public async Task Initialize(CancellationToken cancellationToken = default)
		{
			cancellationToken = WrapCancellationToken(cancellationToken);
			switch (_loadType)
			{
				case LoadType.Lazy:
					break;
				case LoadType.PartiallyOnInitialization:
					await GetOrCreateNodesArray(cancellationToken).WithoutContextCapture();
					break;
				case LoadType.OnInitialization:
					await GetAll(cancellationToken).WithoutContextCapture();
					break;
			}
		}

		public void Stop() => _cancellation.Cancel();

		protected abstract Task<List<T>> GetSubsequenceImpl(Node[] nodes, int start, int count, CancellationToken cancellationToken = default);
		protected abstract Task<ResponseT> GetPage(int startIndex, int count, CancellationToken cancellationToken = default);
		protected abstract int GetTotalFromResponse(ResponseT response);
		protected abstract IList<T> GetValuesFromResponse(ResponseT response);

		protected Task<Node[]> GetOrCreateNodesArray(CancellationToken cancellationToken = default, int? start = null, int? count = null)
		{
			if (start.TryGet(out var startValue) && startValue < 0)
				throw new ArgumentOutOfRangeException($"start: {start} was less than 0");

			if (count.TryGet(out var countValue) && countValue < 0)
				throw new ArgumentOutOfRangeException($"count: {count} was less than 0");

			var currentLoadCompletionSource = _initialLoadCompletionSource.Value;
			if (currentLoadCompletionSource != null)
			{
				if (currentLoadCompletionSource.Task.IsCompletedSuccessfully)
					return _initialLoadCompletionSource.Value.Task;
				if (currentLoadCompletionSource.Task.IsCompleted)
					_initialLoadCompletionSource.AtomicCompareExchange(null, currentLoadCompletionSource);
			}

			return TaskUtils.RunOnce(_initialLoadCompletionSource, async cancellationToken =>
			{
				var startIndex = start ?? 0;
				var batchSize = count.HasValue ? Math.Min(count.Value, _maxBatchSize) : _maxBatchSize;
				var response = await GetPage(startIndex, batchSize, cancellationToken).WithoutContextCapture();
				var foundTotal = GetTotalFromResponse(response);
				var foundValues = GetValuesFromResponse(response);
				var nodes = new Node[foundTotal];
				Enumerable.Range(startIndex, batchSize).Each(index => nodes[index] = new Node() { ContentTask = Task.FromResult(foundValues), IndexInArray = index - startIndex });
				return nodes;
			}, cancellationToken);
		}

		private CancellationToken WrapCancellationToken(CancellationToken cancellationToken)
		{
			var newSource = CancellationTokenSource.CreateLinkedTokenSource(_cancellation.Token, cancellationToken);
			return newSource.Token;
		}

		protected static TaskCompletionSource<R> NewTaskCompletionSource<R>() => new TaskCompletionSource<R>(TaskCreationOptions.RunContinuationsAsynchronously);

		protected static TaskCompletionSource<Node[]> NewTaskCompletionSourceWithKnownArraySize(int knownTotal, IList<T> initialContents = null)
		{
			var newSource = NewTaskCompletionSource<Node[]>();
			var arr = new Node[knownTotal];
			if (initialContents != null)
				initialContents.Each((item, index) => arr[index] = new Node { ContentTask = Task.FromResult(initialContents), IndexInArray = index });
			newSource.SetResult(arr);
			return newSource;
		}

		protected class Node
		{
			internal Task<IList<T>> ContentTask;
			internal int IndexInArray;
		}
	}

	public abstract class IndexedQueryCache<T, ResponseT> : QueryCache<T, ResponseT>, IQueryCache<T>
	{
		public IndexedQueryCache(int maxBatchSize, LoadType loadType = LoadType.Lazy, int? knownTotal = null, IList<T> knownInitialValues = null) : base(maxBatchSize, loadType, knownTotal, knownInitialValues)
		{
		}

		protected override async Task<List<T>> GetSubsequenceImpl(Node[] nodes, int start, int count, CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var (allTasks, batches) = GetTasksToAwait(nodes, start, count, _maxBatchSize);
			if (batches.Any())
			{
				var batchTasks = batches.Select(batch => TaskUtils.RunAndNotify(batch.BatchCompletionSwitch, async cancellationToken =>
				{
					var response = await GetPage(batch.BatchStart, Math.Min(_maxBatchSize, nodes.Length - batch.BatchStart), cancellationToken).WithoutContextCapture();
					return GetValuesFromResponse(response);
				}, cancellationToken)).Select(SwallowExceptions).ToArray();
				await Task.WhenAll(batchTasks).WithoutContextCapture();
			}
			var returnList = new List<T>(count);
			foreach (var (task, indexInArray, neededFetching) in allTasks)
			{
				try
				{
					returnList.Add((await task.WithoutContextCapture())[indexInArray]);
				}
				catch (Exception) when (!neededFetching)
				{
					return await GetSubsequence(start, count, cancellationToken).WithoutContextCapture();
				}
			}
			return returnList;
		}

		private static (FetchState[] tasks, List<BatchInfo> batches) GetTasksToAwait(Node[] array, int startIndex, int count, int batchSize)
		{
			var taskList = new FetchState[count];
			var batches = new List<BatchInfo>((count / batchSize) + 1);
			TaskCompletionSource<IList<T>> newCompletionSource = null;
			var goal = Math.Min(startIndex + count, array.Length);
			for (int i = startIndex; i < goal; i++)
			{
				var newBatch = !batches.Any() || batches.Last().BatchStart + batchSize <= i;
				bool needsFetching = false;
				Node node = array[i];
				Node nodeToUse = null;
				Node newNode = null;
				while (nodeToUse == null)
				{
					if (node == null)
					{
						if (newBatch)
							newCompletionSource = NewTaskCompletionSource<IList<T>>();
						var indexInArray = newBatch ? 0 : i - batches.Last().BatchStart;
						newNode ??= new Node { ContentTask = newCompletionSource.Task, IndexInArray = indexInArray };
						if (Interlocked.CompareExchange(ref array[i], newNode, null) == null)
						{
							nodeToUse = newNode;
							needsFetching = true;
							if (newBatch)
								batches.Add(new(i, newCompletionSource));
						}
					}
					else if (node.ContentTask.IsCanceled || node.ContentTask.IsFaulted)
						Interlocked.CompareExchange(ref array[i], null, node);
					else
					{
						nodeToUse = node;
						needsFetching = false;
					}
				}
				taskList[i - startIndex] = new(nodeToUse.ContentTask, nodeToUse.IndexInArray, needsFetching);
			}
			return (taskList, batches);
		}

		private static async Task SwallowExceptions(Task task)
		{
			try
			{
				await task.WithoutContextCapture();
			}
			catch (Exception) { }
		}

		private readonly record struct FetchState(Task<IList<T>> Task, int index, bool NeedsFetching);
		private readonly record struct BatchInfo(int BatchStart, TaskCompletionSource<IList<T>> BatchCompletionSwitch);
	}
}