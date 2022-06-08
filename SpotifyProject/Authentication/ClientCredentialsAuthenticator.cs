using System;
using System.Threading;
using System.Threading.Tasks;
using ApplicationResources.Services;
using CustomResources.Utils.Concepts;
using CustomResources.Utils.Concepts.DataStructures;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;
using SpotifyAPI.Web;
using SpotifyProject.SpotifyAdditions;
using Util = CustomResources.Utils.GeneralUtils.Utils;

namespace SpotifyProject.Authentication
{
	/**
	 * A basic authenticator that only uses the client id and secret. This authenticator can not be used in a situation where access to spotify resources that require scopes (such
	 * as user information) is required
	 */
	public class ClientCredentialsAuthenticator : ISpotifyAuthenticator
	{
		public event TaskUtils.AsyncEvent<IAuthenticator> OnLoggedIn;

		protected ClientCredentialsAuthenticator()
		{
			OnLoggedIn += (result, _) =>
			{
				CurrentAuthenticator = result;
				return Task.CompletedTask;
			};
		}

		public bool IsLoggedIn => _isLoggedIn;
		public Task<bool> GetIsLoggedIn(CancellationToken cancellationToken = default) => Task.FromResult(true);

		public async Task<Result<IAuthenticator>> LogIn(IOAuthClientInfo authorizationSource, CancellationToken cancellationToken = default)
		{
			Result<IAuthenticator> result = default;
			var didLogIn = await Util.LoadOnceBlockingAsync(_isLoggedIn, _lock, _ =>
			{
				var authenticator = new SpotifyAPI.Web.ClientCredentialsAuthenticator(authorizationSource.ClientId, authorizationSource.ClientSecret);
				result = new(authenticator);
				return Task.CompletedTask;
			}, cancellationToken);
			if (didLogIn && OnLoggedIn != null)
				await OnLoggedIn.InvokeAsync(result.ResultValue, cancellationToken).WithoutContextCapture();
			return result;
		}

		Task<Result<IAuthenticator>> IAuthenticationService<AuthorizationSource, IAuthenticator>.LogIn(AuthorizationSource userInfo, CancellationToken cancellationToken) =>
			LogIn(userInfo, cancellationToken);

		public IAuthenticator CurrentAuthenticator
		{
			get
			{
				if (!_isLoggedIn)
					throw new InvalidOperationException("Cannot get an authenticator when not logged in");
				return _currentAuthenticator;
			}
			protected set
			{
				_currentAuthenticator = value;
			}
		}
		private IAuthenticator _currentAuthenticator;

		protected MutableReference<bool> _isLoggedIn;
		private readonly AsyncLockProvider _lock = new();
	}
}