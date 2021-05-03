using System;
using SpotifyAPI.Web;

namespace SpotifyProject
{
    public class SpotifyCommunicator
    {
        protected readonly SpotifyClientConfig _config;

        public SpotifyCommunicator(SpotifyClientConfig config)
        {
            _config = config;
        }
    }
}
