using System;
using System.Threading;
using System.Threading.Tasks;
using CustomResources.Utils.Extensions;

namespace CustomResources.Utils.Concepts.DataStructures
{
	public abstract class Flusher<FlushableT, ContainerT> : TaskContainingDisposable where ContainerT : class, IFlushableContainer<FlushableT>
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

		public void Add(FlushableT item)
		{
			var currentContainer = _currentFlushableContainer;
			currentContainer.Update(item);
			ScheduleFlush();
		}

		private void ScheduleFlush()
		{
			if (_currentFlushableContainer.RequestFlush())
				Run(ConductFlush);
		}

		private async Task ConductFlush()
		{
			await Task.Delay(_flushWaitTime, StopToken).WithoutContextCapture();

			var newContainer = CreateNewContainer();
			var oldContainer = Interlocked.Exchange(ref _currentFlushableContainer, newContainer);

			var additionalFlushNeeded = await Flush(oldContainer).WithoutContextCapture();
			if (((int)additionalFlushNeeded).AsBool())
				ScheduleFlush();
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
				Flush(container);
		}
	}

	public interface IFlushableContainer<FlushableT>
	{
		bool Update(FlushableT itemToFlush);
		bool RequestFlush();
	}
}
