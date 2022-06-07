using System;
using System.Threading;
using System.Threading.Tasks;
using CustomResources.Utils.Extensions;

namespace CustomResources.Utils.GeneralUtils
{
	public static class TaskUtils
	{
		public delegate Task AsyncEvent(CancellationToken cancellationToken);
		public delegate Task AsyncEvent<ArgsT>(ArgsT eventArgs, CancellationToken cancellationToken);
		public delegate Task AsyncEvent<SenderT, ArgsT>(SenderT sender, ArgsT eventArgs, CancellationToken cancellationToken);

		public static Func<Task> Compose(Action action, Func<Task> asyncAction)
		{
			Ensure.ArgumentNotNull(asyncAction, nameof(asyncAction));
			Ensure.ArgumentNotNull(action, nameof(action));
			return async () => { await asyncAction().WithoutContextCapture(); action(); };
		}

		public static Func<Task<R>> Compose<T, R>(Func<T, R> func, Func<Task<T>> asyncFunc)
		{
			Ensure.ArgumentNotNull(asyncFunc, nameof(asyncFunc));
			Ensure.ArgumentNotNull(func, nameof(func));
			return async () => func(await asyncFunc().WithoutContextCapture());
		}

		public static async Task<R> SupplyAfter<R>(Func<R> supplier, TimeSpan delay, CancellationToken cancellationToken = default)
		{
			Ensure.ArgumentNotNull(supplier, nameof(supplier));
			await Task.Delay(delay, cancellationToken).WithoutContextCapture();
			return supplier();
		}
	}
}