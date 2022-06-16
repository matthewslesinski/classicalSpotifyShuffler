using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using SpotifyProject.Utils;
using System;
using CustomResources.Utils.Extensions;
using ApplicationResources.Logging;
using ApplicationResources.Services;
using SpotifyProject.Configuration;
using ApplicationResources.ApplicationUtils.Parameters;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SpotifyProjectTests")]


namespace SpotifyProject.SpotifyPlaybackModifier
{
    public class SpotifyPlaybackReorderer : SpotifyAccessorBase
    {
        private readonly SpotifyCommandExecutor _commandExecutor;
        public SpotifyPlaybackReorderer(SpotifyClient spotify) : base(spotify)
        {
            _commandExecutor = new(spotify);
        }

        public async Task ShuffleUserProvidedContext()
		{
            Task<bool> TryHandleUserInput(string input)
			{
                if (string.Equals(input.Trim(), "current", StringComparison.OrdinalIgnoreCase))
                    return ShuffleCurrentPlayback();
                if (SpotifyDependentUtils.TryParseUriFromLink(input, out var uri))
                    input = uri;
                if (input.StartsWith(SpotifyConstants.SpotifyUriPrefix))
                    return _commandExecutor.ModifyContext(input);
                if (Enum.TryParse<PlaybackContextType>(input, true, out var contextType) && contextType == PlaybackContextType.AllLikedTracks)
                    return _commandExecutor.ModifyContext(contextType, null);
                else
                    return Task.FromResult(false);
			}

            var ui = this.AccessUserInterface();
            var userInput = await ui.RequestResponseAsync("Please provide the indicator of the Spotify context to use").WithoutContextCapture();
            while (!await TryHandleUserInput(userInput).WithoutContextCapture())
			{
                var response = await ui.RequestResponseAsync("The provided input was not valid. Would you like to try again? If so, you can just provide new input now. " +
                    "Otherwise we may default to the current track's album, if that was a startup argument provided").WithoutContextCapture();
                var parsedAffirmation = ui.ParseConfirmation(response);
                if (parsedAffirmation.HasValue)
				{
                    if (parsedAffirmation.Value)
                        response = await ui.RequestResponseAsync("Please provide a new Uri").WithoutContextCapture();
                    else
                    {
                        if (TaskParameters.Get<bool>(SpotifyParameters.DefaultToAlbumShuffle))
                            await ShuffleCurrentAlbumOfCurrentTrack().WithoutContextCapture();
                        return;
                    }
				}
                userInput = response;
			}
		}

        public async Task<bool> ShuffleCurrentPlayback()
		{
            var currentlyPlaying = await this.GetCurrentlyPlaying().WithoutContextCapture();
            var contextUri = currentlyPlaying.Context?.Uri;
            bool result = contextUri != null && await _commandExecutor.ModifyContext(contextUri).WithoutContextCapture();
            if (result)
                return result;
            if (TaskParameters.Get<bool>(SpotifyParameters.DefaultToAlbumShuffle))
                return await ShuffleCurrentAlbumOfCurrentTrack().WithoutContextCapture();
            Logger.Error("Playback could not be modified because the current context is unrecognized");
            return false;
        }

        public async Task<bool> ShuffleCurrentAlbumOfCurrentTrack()
        {
            var currentlyPlaying = await this.GetCurrentlyPlaying().WithoutContextCapture();
            var album = ((FullTrack)currentlyPlaying.Item).Album;
            if (album.Id == null)
            {
                Logger.Warning("Could not shuffle the current album because it is a local track that does not have an official album");
                return false;
            }
            else
                return await _commandExecutor.ModifyContext(PlaybackContextType.Album, album.Id).WithoutContextCapture();            
        }
    }
}
