using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using CustomResources.Utils.Extensions;

namespace CustomResources.Utils.Concepts.DataStructures
{
	public abstract class Flusher<FlushableT, ContainerT> : TaskContainingDisposable<bool> where ContainerT : class, IFlushableContainer<FlushableT>
	{
		private readonly bool _flushOnDestroy;
		private readonly TimeSpan _flushWaitTime;

		protected ContainerT _currentFlushableContainer;

		public Flusher(TimeSpan flushWaitTime, bool flushOnDestroy, CancellationToken cancellationToken = default) : base(cancellationToken)
		{
			_flushOnDestroy = flushOnDestroy;
			_flushWaitTime = flushWaitTime;
			_currentFlushableContainer = CreateNewContainer();
			if (_flushOnDestroy)
				AppDomain.CurrentDomain.ProcessExit += DoFlushOnClose;
		}

		protected enum AdditionalFlushOptions
		{
			NeedsAdditionalFlush = 1,
			NoAdditionalFlushNeeded = 0
		}

		// Returned bool should indicate if additional flushing is necessary
		protected abstract Task<AdditionalFlushOptions> Flush(ContainerT containerToFlush);
		protected abstract ContainerT CreateNewContainer();
		protected abstract bool OnFlushFailed(Exception e);

		public void Add(FlushableT item)
		{
			var currentContainer = _currentFlushableContainer;
			currentContainer.Update(item);
			_ = ScheduleFlush();
		}

		private async Task ScheduleFlush()
		{
			if (_currentFlushableContainer.RequestFlush())
			{
				var result = await Run(ConductFlush).WithoutContextCapture();
				if (result.Success && (result.ResultValue || _currentFlushableContainer.IsReadyToBeFlushed))
					_ = ScheduleFlush();
			}
		}

		private async Task<bool> ConductFlush()
		{
			await Task.Delay(_flushWaitTime, StopToken).WithoutContextCapture();

			var newContainer = CreateNewContainer();
			var oldContainer = Interlocked.Exchange(ref _currentFlushableContainer, newContainer);

			return await DoFlush(oldContainer).WithoutContextCapture();
		}

		private async Task<bool> DoFlush(ContainerT container)
		{
			try
			{
				var additionalFlushNeeded = await Flush(container).WithoutContextCapture();
				return ((int)additionalFlushNeeded).AsBool();
			}
			catch (Exception e) when (!(e is OperationCanceledException))
			{
				if (OnFlushFailed(e))
					throw;
				else
					return false;
			}
		}

		protected override void DoDispose()
		{
			if (_flushOnDestroy)
			{
				AppDomain.CurrentDomain.ProcessExit -= DoFlushOnClose;
				DoFlushOnClose(null, null);
			}
			base.DoDispose();
		}

		private void DoFlushOnClose(object sender, EventArgs args)
		{
			var container = Interlocked.Exchange(ref _currentFlushableContainer, null);
			// Only flush if one has already been scheduled, so requesting to flush should actually be false
			if (!container.RequestFlush())
				_ = DoFlush(container);
		}
	}

	public interface IFlushableContainer<FlushableT>
	{
		bool Update(FlushableT itemToFlush);
		bool RequestFlush();
		bool IsReadyToBeFlushed { get; }
	}

	public class CombiningContainer<FlushableT, OutputT> : IFlushableContainer<FlushableT>
	{
		private OutputT _output;
		private Reference<bool> _isFlushScheduled = false;
		private Func<OutputT, FlushableT, OutputT> _combiner;

		public CombiningContainer(Func<OutputT, FlushableT, OutputT> combiner, OutputT seed = default)
		{
			_output = seed;
			_combiner = combiner;
		}

		public bool IsReadyToBeFlushed { get; private set; }

		public OutputT Contents => _output;

		public bool RequestFlush() => GeneralUtils.Utils.IsFirstRequest(ref _isFlushScheduled);

		public bool Update(FlushableT itemToFlush)
		{
			_output = Combine(_output, itemToFlush);
			IsReadyToBeFlushed = true;
			return true;
		}

		protected OutputT Combine(OutputT existing, FlushableT incoming) => _combiner(existing, incoming);
	}

	public class ReplacingContainer<FlushableT> : CombiningContainer<FlushableT, FlushableT>
	{
		public ReplacingContainer() : base((existing, incoming) => incoming) { }
	}
}
