using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SpotifyAPI.Web;

namespace SpotifyProject.Authentication
{
    /**
     * Abstracts out the authentication process
     */
    public abstract class Authenticator : SpotifyCommunicator
    {
        public Authenticator(SpotifyClientConfig config) : base(config)
        {
        }

        protected abstract Task<IAuthenticator> GetAuthenticator(AuthorizationSource authorizationSource);

		public async Task<SpotifyClient> Authenticate(AuthorizationSource authorizationSource)
		{
            var authenticator = await GetAuthenticator(authorizationSource);
            return new SpotifyClient(_config.WithAuthenticator(authenticator));
		}

        public static async Task<ClientInfo> ReadClientInfoPath(string clientInfoPath)
        {
            Logger.Verbose($"Reading Client Id and Secret from {clientInfoPath}");
            return JsonConvert.DeserializeObject<ClientInfo>(await File.ReadAllTextAsync(clientInfoPath));
        }
    }
}
