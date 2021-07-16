using System;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;
using APIConnectorConstructor = SpotifyProject.Authentication.APIConnectors.APIConnectorConstructor;

namespace SpotifyProject.SpotifyAdditions
{
	public class SpotifyClientConfigHolder
	{
		public event Action FinishedBuilding;
		public APIConnectorConstructor APIConnectorConstructor { get; private set; }
		public SpotifyClientConfig UnderlyingSpotifyClientConfig => _currentClientConfig;

		private SpotifyClientConfig _currentClientConfig;
		private bool _isFinishedBuilding = false;

		public SpotifyClientConfigHolder(SpotifyClientConfig config)
		{
			_currentClientConfig = config;
			FinishedBuilding += SetAPIConnectorFromConstructor;
		}

		public SpotifyClientConfigHolder WithToken(string token, string tokenType = "Bearer")
		{
			CheckNotFinalizedAlready();
			_currentClientConfig = _currentClientConfig.WithToken(token, tokenType);
			return this;
		}

		public SpotifyClientConfigHolder WithRetryHandler(IRetryHandler retryHandler)
		{
			CheckNotFinalizedAlready();
			_currentClientConfig = _currentClientConfig.WithRetryHandler(retryHandler);
			return this;
		}

		public SpotifyClientConfigHolder WithAuthenticator(IAuthenticator authenticator)
		{
			CheckNotFinalizedAlready();
			_currentClientConfig = _currentClientConfig.WithAuthenticator(authenticator);
			return this;
		}

		public SpotifyClientConfigHolder WithHTTPLogger(IHTTPLogger httpLogger)
		{
			CheckNotFinalizedAlready();
			_currentClientConfig = _currentClientConfig.WithHTTPLogger(httpLogger);
			return this;
		}

		public SpotifyClientConfigHolder WithHTTPClient(IHTTPClient httpClient)
		{
			CheckNotFinalizedAlready();
			_currentClientConfig = _currentClientConfig.WithHTTPClient(httpClient);
			return this;
		}

		public SpotifyClientConfigHolder WithJSONSerializer(IJSONSerializer jsonSerializer)
		{
			CheckNotFinalizedAlready();
			_currentClientConfig = _currentClientConfig.WithJSONSerializer(jsonSerializer);
			return this;
		}

		public SpotifyClientConfigHolder WithDefaultPaginator(IPaginator defaultPaginator)
		{
			CheckNotFinalizedAlready();
			_currentClientConfig = _currentClientConfig.WithDefaultPaginator(defaultPaginator);
			return this;
		}

		public SpotifyClientConfigHolder WithAPIConnectorConstructor(APIConnectorConstructor apiConnectorConstructor)
		{
			CheckNotFinalizedAlready();
			APIConnectorConstructor = apiConnectorConstructor;
			return this;
		}

		public SpotifyClientConfigHolder Finalized() {
			CheckNotFinalizedAlready();
			if (FinishedBuilding != null)
				FinishedBuilding();
			_isFinishedBuilding = true;
			return this;
		}

		private void SetAPIConnectorFromConstructor()
		{
			if (_currentClientConfig.APIConnector == null && APIConnectorConstructor != null)
				_currentClientConfig = _currentClientConfig.WithAPIConnector(APIConnectorConstructor(_currentClientConfig.BaseAddress, _currentClientConfig.Authenticator, _currentClientConfig.JSONSerializer, _currentClientConfig.HTTPClient, _currentClientConfig.RetryHandler, _currentClientConfig.HTTPLogger));
		}

		private void CheckNotFinalizedAlready() {
			if (_isFinishedBuilding)
				throw new Exception("Cannot continue building after finalized");
		}

		public static SpotifyClientConfigHolder CreateWithDefaultConfig() => new SpotifyClientConfigHolder(SpotifyClientConfig.CreateDefault());
	}
}
