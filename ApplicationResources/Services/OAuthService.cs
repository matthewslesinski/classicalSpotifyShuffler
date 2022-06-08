using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ApplicationResources.Logging;
using CustomResources.Utils.Concepts;
using CustomResources.Utils.Concepts.DataStructures;
using CustomResources.Utils.Extensions;

namespace ApplicationResources.Services
{
	public abstract class ManualAuthorizationCodeAuthenticatorService<AuthArgsT, AuthTokensT, SessionInfoT, AuthResultT>
		: AccountAuthenticationService<AuthArgsT, AuthResultT>, IAuthCodeAuthenticationService<AuthArgsT, AuthResultT>
		where AuthArgsT : IAuthCodeArgs
		where AuthTokensT : IOAuthTokens
		where SessionInfoT : class, IOAuthInfo<AuthArgsT, AuthTokensT>
	{
		public async Task<Result<AuthResultT>> TryImmediateLogIn(CancellationToken cancellationToken = default)
		{
			var existingSessionResult = await GetExistingSession(cancellationToken).WithoutContextCapture();
			if (!existingSessionResult.DidFind)
				return Result<AuthResultT>.Failure;
			return await LogIn(existingSessionResult.ResultValue.AuthSource, cancellationToken).WithoutContextCapture();
		}

		protected override async Task<Result<AuthResultT>> DoLogIn(AuthArgsT userInfo, CancellationToken cancellationToken = default)
		{
			var isOnlyActiveAttempt = _lock.TryAcquireToken(out var acquiredLock);
			if (!isOnlyActiveAttempt)
			{
				Logger.Warning("Attempting to log in while a current log in/out attempt is in progress");
				return Result<AuthResultT>.Failure;
			}
			using (acquiredLock)
			{
				var (savedSessionExists, currentSession) = await GetExistingSession(cancellationToken).WithoutContextCapture();
				var createNewSession = !savedSessionExists || !Equals(currentSession.AuthSource, userInfo);
				if (createNewSession)
				{
					if (savedSessionExists)
						await PersistCurrentSession(null, cancellationToken).WithoutContextCapture();
					var tokens = await RequestNewTokens(userInfo).WithoutContextCapture();
					currentSession = BuildSessionInfo(userInfo, tokens);
				}
				var authenticatedRepresentation = await GetAuthenticatedOutputFromTokens(currentSession, cancellationToken).WithoutContextCapture();
				if (createNewSession)
					_ = PersistCurrentSession(currentSession, cancellationToken);
				return new(true, authenticatedRepresentation);
			}
		}

		protected override async Task<bool> DoLogOut(CancellationToken cancellationToken = default)
		{
			var isOnlyActiveAttempt = _lock.TryAcquireToken(out var acquiredLock);
			if (!isOnlyActiveAttempt)
			{
				Logger.Warning("Attempting to log in while a current log in/out attempt is in progress");
				return false;
			}
			using (acquiredLock)
			{
				await PersistCurrentSession(null, cancellationToken).WithoutContextCapture();
				return true;
			}
		}

		private async Task<AuthTokensT> RequestNewTokens(AuthArgsT userInfo)
		{
			var loginUri = DetermineLoginAddress(userInfo);
			var authorizationCode = await RequestLoginFromUser(loginUri).WithoutContextCapture();
			var response = await AcquireTokens(userInfo, authorizationCode).WithoutContextCapture();
			return response;
		}

		protected abstract Uri DetermineLoginAddress(AuthArgsT userInfo);
		protected abstract Task<string> RequestLoginFromUser(Uri loginUri, CancellationToken cancellationToken = default);
		protected abstract Task<AuthTokensT> AcquireTokens(AuthArgsT userInfo, string authCode, CancellationToken cancellationToken = default);
		protected abstract SessionInfoT BuildSessionInfo(AuthArgsT userInfo, AuthTokensT tokens);
		protected abstract Task<AuthResultT> GetAuthenticatedOutputFromTokens(SessionInfoT sessionInfo, CancellationToken cancellationToken = default);

		protected abstract Task<Result<SessionInfoT>> GetExistingSession(CancellationToken cancellationToken = default);
		protected abstract Task PersistCurrentSession(SessionInfoT sessionInfo, CancellationToken cancellationToken = default);

		protected readonly AsyncLockProvider _lock = new();
	}

	public abstract class ClientCredentialsAuthenticatorService<AuthResultT> : AuthenticationService<IOAuthClientInfo, AuthResultT>, IOAuthService<IOAuthClientInfo, AuthResultT>
	{
		public override Task<bool> GetIsLoggedIn(CancellationToken cancellationToken = default) => Task.FromResult(_currentClientInfo != null);

		protected override Task<Result<AuthResultT>> DoLogIn(IOAuthClientInfo userInfo, CancellationToken cancellationToken = default)
		{
			_currentClientInfo = new(userInfo);
			return Task.FromResult<Result<AuthResultT>>(new(true, GetLoggedInRepresentation(userInfo)));
		}

		protected abstract AuthResultT GetLoggedInRepresentation(IOAuthClientInfo clientInfo);
		// This contains sensitive information, so be wary about exposing it to outside classes
		private Reference<IOAuthClientInfo> _currentClientInfo;
	}

	public interface IAuthCodeAuthenticationService<in AuthArgsT, AuthResultT> : IOAuthService<AuthArgsT, AuthResultT>, IAccountAuthenticationService<AuthArgsT, AuthResultT>
		where AuthArgsT : IAuthCodeArgs
	{ }

	public interface IOAuthService<in AuthArgsT, AuthResultT> : IAuthenticationService<AuthArgsT, AuthResultT>
		where AuthArgsT : IOAuthClientInfo
	{ }

	public interface IOAuthClientInfo
	{
		string ClientId { get; }
		string ClientSecret { get; }
	}

	public interface IAuthCodeArgs : IOAuthClientInfo
	{
		public Uri RedirectUri { get; }
		public ICollection<string> Scopes { get; }
	}

	public interface IOAuthTokens
	{
		string AccessToken { get; }
		string RefreshToken { get; }
	}

	public interface IOAuthInfo<AuthArgsT, AuthTokensT> where AuthArgsT : IAuthCodeArgs where AuthTokensT : IOAuthTokens
	{
		AuthArgsT AuthSource { get; }
		AuthTokensT AuthTokens { get; }
	}
}