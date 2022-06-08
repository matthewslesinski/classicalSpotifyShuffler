using System;
using System.Threading;
using System.Threading.Tasks;
using ApplicationResources.Logging;
using CustomResources.Utils.Concepts;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;

namespace ApplicationResources.Services
{
	public abstract class AuthenticationService<AuthArgsT, AuthResultT> : IAuthenticationService<AuthArgsT, AuthResultT>
	{
		public event TaskUtils.AsyncEvent<AuthResultT> OnLoggedIn;

		public async Task<Result<AuthResultT>> LogIn(AuthArgsT userInfo, CancellationToken cancellationToken = default)
		{
			async Task<Result<AuthResultT>> PerformLogIn()
			{
				if (await GetIsLoggedIn(cancellationToken).WithoutContextCapture())
					return Result<AuthResultT>.Failure;
				return await DoLogIn(userInfo, cancellationToken).WithoutContextCapture();
			}
			try
			{

				var result = await PerformLogIn();
				if (result.Success && OnLoggedIn != null)
					await OnLoggedIn.InvokeAsync(result.ResultValue, cancellationToken);
				return result;
			}
			catch (Exception e)
			{
				var shouldThrow = OnLogInError(e);
				if (shouldThrow)
					throw;
				else
					return Result<AuthResultT>.Failure;
			}
		}

		protected virtual bool OnLogInError(Exception e)
		{
			Logger.Error("An exception occurred during logging in: {exception}", e);
			return true;
		}

		public abstract Task<bool> GetIsLoggedIn(CancellationToken cancellationToken = default);
		protected abstract Task<Result<AuthResultT>> DoLogIn(AuthArgsT userInfo, CancellationToken cancellationToken = default);
	}

	public abstract class AccountAuthenticationService<AuthArgsT, AuthResultT> : AuthenticationService<AuthArgsT, AuthResultT>, IAccountAuthenticationService<AuthArgsT, AuthResultT>
	{
		public event TaskUtils.AsyncEvent OnLoggedOut;

		public async Task<bool> LogOut(CancellationToken cancellationToken = default)
		{
			async Task<bool> PerformLogOut()
			{
				if (!await GetIsLoggedIn(cancellationToken).WithoutContextCapture())
					return false;
				return await DoLogOut(cancellationToken).WithoutContextCapture();
			}
			try
			{
				var didPerformALogOut = await PerformLogOut();
				if (didPerformALogOut && OnLoggedOut != null)
					await OnLoggedOut.InvokeAsync(cancellationToken);
				return didPerformALogOut;
			}
			catch (Exception e)
			{
				var shouldThrow = OnLogOutError(e);
				if (shouldThrow)
					throw;
				else
					return false;
			}
		}

		protected virtual bool OnLogOutError(Exception e)
		{
			Logger.Error("An exception occurred during logging out: {exception}", e);
			return true;
		}

		protected abstract Task<bool> DoLogOut(CancellationToken cancellationToken = default);
	}

	public interface IAuthenticationService<in AuthArgsT, AuthResultT> : IGlobalServiceUser
	{
		public event TaskUtils.AsyncEvent<AuthResultT> OnLoggedIn;
		public Task<Result<AuthResultT>> LogIn(AuthArgsT userInfo, CancellationToken cancellationToken = default);
		public Task<bool> GetIsLoggedIn(CancellationToken cancellationToken = default);
	}

	public interface IAccountAuthenticationService<in AuthArgsT, AuthResultT> : IAuthenticationService<AuthArgsT, AuthResultT>
	{
		public event TaskUtils.AsyncEvent OnLoggedOut;
		public Task<bool> LogOut(CancellationToken cancellationToken = default);
	}
}

