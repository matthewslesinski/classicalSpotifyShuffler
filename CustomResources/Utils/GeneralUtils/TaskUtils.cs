using System;
using System.Threading;
using System.Threading.Tasks;
using CustomResources.Utils.Concepts.DataStructures;
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

		public static Task<R> RunOnce<R>(MutableClassReference<TaskCompletionSource<R>> subscriptionSource, Func<CancellationToken, Task<R>> action,
			CancellationToken cancellationToken = default, TaskCreationOptions creationOptions = TaskCreationOptions.RunContinuationsAsynchronously)
		{
			if (subscriptionSource.Value == null && subscriptionSource.AtomicCompareExchange(new TaskCompletionSource<R>(creationOptions), null) == null)
				return RunAndNotify(subscriptionSource, action, cancellationToken);
			var currentSubscriptionSource = subscriptionSource.Value;
			// check if it's null to make sure it hasn't been switched out on us
			if (currentSubscriptionSource == null)
				return RunOnce(subscriptionSource, action, cancellationToken, creationOptions);
			var subscribedTask = currentSubscriptionSource.Task;
			if (subscribedTask.IsCompleted && !subscribedTask.IsCanceled)
				return subscribedTask;

			async Task<R> AwaitOtherAction()
			{
				try
				{
					return await subscribedTask.WaitAsync(cancellationToken).WithoutContextCapture();
				}
				catch (OperationCanceledException e) when (e.CancellationToken != cancellationToken)
				{
					return await RunOnce(subscriptionSource, action, cancellationToken, creationOptions).WithoutContextCapture();
				}
				catch (OperationCanceledException e) when (e.CancellationToken == cancellationToken)
				{
					if (subscriptionSource.AtomicCompareExchange(null, currentSubscriptionSource) != currentSubscriptionSource)
						throw new InvalidOperationException("Somehow the subscription source was already modified");
					throw;
				}
			}
			return AwaitOtherAction();
		}

		public static async Task<R> RunAndNotify<R>(TaskCompletionSource<R> subscription, Func<CancellationToken, Task<R>> action, CancellationToken cancellationToken = default)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				subscription.SetCanceled(cancellationToken);
				cancellationToken.ThrowIfCancellationRequested();
			}
			if (subscription.Task.IsCompleted)
				throw new ArgumentException("To run a task, it must not be completed yet");
			try
			{
				var result = await action(cancellationToken).WaitAsync(cancellationToken).WithoutContextCapture();
				subscription.SetResult(result);
				return result;
			}
			catch (OperationCanceledException e)
			{
				subscription.SetCanceled(e.CancellationToken);
				throw;
			}
			catch (Exception e)
			{
				subscription.SetException(e);
				throw;
			}
		}
	}
}