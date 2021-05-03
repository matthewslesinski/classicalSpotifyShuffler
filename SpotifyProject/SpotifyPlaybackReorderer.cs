using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackModifiers;
using SpotifyProject.SpotifyPlaybackModifier.Transformations;
using SpotifyProject.SpotifyPlaybackModifier;
using SpotifyProject.Utils;
using System.Linq;
using System;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;
using SpotifyProject.Setup;

namespace SpotifyProject
{
    public class SpotifyPlaybackReorderer
    {

        public SpotifyPlaybackReorderer(SpotifyClient spotify, bool shouldDefaultToAlbumReordering)
        {
            _spotify = spotify;
            _configuration = new SpotifyConfiguration { Spotify = _spotify, Market = "US" };
            _shouldDefaultToAlbumReordering = shouldDefaultToAlbumReordering;
        }

        public async Task ShuffleUserProvidedContext()
		{
            UserInterface.Instance.NotifyUser("Please provide the Uri of the Spotify context to use");
            var contextUri = UserInterface.Instance.ReadNextUserInput();
            while (!await ModifyContext(contextUri))
			{
                UserInterface.Instance.NotifyUser("The provided context Uri was not valid. Would you like to try again? If so, you can just provide a new Id. " +
					"Otherwise we may default to the current track's album, if that was a startup argument provided");
                var response = UserInterface.Instance.ReadNextUserInput();
                var parsedAffirmation = UserInterface.Instance.ParseAffirmation(response);
                if (parsedAffirmation.HasValue)
				{
                    if (parsedAffirmation.Value)
                        UserInterface.Instance.NotifyUser("Please provide a new Uri");
                    else
                    {
                        if (_shouldDefaultToAlbumReordering)
                            await ShuffleCurrentAlbumOfCurrentTrack();
                        return;
                    }
				}
                contextUri = response;
			}
		}

        public async Task ShuffleCurrentPlayback()
		{
            var currentlyPlaying = await _spotify.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest { Market = _configuration.Market });
            var contextUri = currentlyPlaying.Context?.Uri;
            bool result = contextUri != null && await ModifyContext(contextUri);
            if (result)
                return;
            if (_shouldDefaultToAlbumReordering)
                await ShuffleCurrentAlbumOfCurrentTrack();
            else
                Logger.Error("Playback could not be modified because the current context is unrecognized");
        }

        public async Task ShuffleCurrentAlbumOfCurrentTrack()
        {
            var currentlyPlaying = await _spotify.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest { Market = _configuration.Market });
            var album = ((FullTrack)currentlyPlaying.Item).Album;
            if (album.Id == null)
                Logger.Warning("Could not shuffle the current album because it is a local track that does not have an official album");
            else
                await ModifyContext<FullAlbum, SimpleTrack>(PlaybackContextType.Album, album.Id);            
        }

        private bool TryParseContextTypeFromUri(string contextUri, out PlaybackContextType contextType, out string contextId)
		{
            contextType = default;
            if (!SpotifyDependentUtils.TryParseSpotifyUri(contextUri, out var typeString, out contextId, out var allParts))
                return false;
            if (allParts.Length > 3)
                Logger.Warning($"The contextUri received from spotify was of an unfamiliar form, but could still be parsed: {contextUri}");
            return Enum.TryParse(typeString, true, out contextType);
		}

        private async Task<bool> ModifyContext(string contextUri)
		{
            if (!TryParseContextTypeFromUri(contextUri, out var contextType, out var contextId))
                return false;
            bool result;
            switch (contextType)
            {
                case PlaybackContextType.Album:
                    result = await ModifyContext<FullAlbum, SimpleTrack>(contextType, contextId);
                    break;
				case PlaybackContextType.Artist:
					result = await ModifyContext<FullArtist, SimpleTrackAndAlbumWrapper>(contextType, contextId);
					break;
				case PlaybackContextType.Playlist:
                    result = await ModifyContext<FullPlaylist, FullTrack>(contextType, contextId);
                    break;
                default:
                    throw new ArgumentException($"Code should not be able to reach here. Please make sure the {nameof(PlaybackContextType)} type's value of " +
                        $"{contextType} is supported in {nameof(SpotifyPlaybackReorderer)}");
            }
            if (!result)
                Logger.Warning($"Failed to modify playback for context type \"{contextType}\" and id \"{contextId}\"");
            return result;
        }

        private async Task<bool> ModifyContext<SpotifyItemT, TrackT>(PlaybackContextType contextType, string contextId)
        {
            try
            {
                Logger.Information($"Attempting to modify context of type {contextType} and with id {contextId}");
                if (!PlaybackTransformations.TryGetTransformation<SpotifyItemT, TrackT>(contextType, out var transformations))
                {
                    Logger.Warning($"There was no transformation set found for the context type {contextType}");
                    return false;
                }
                if (!PlaybackContexts.TryGetExistingContextConstructorForType<SpotifyItemT, TrackT>(contextType, out var initialContextConstructor))
                {
                    Logger.Warning($"There was no initial context constructor found for the context type {contextType}");
                    return false;
                }
                var transformation = transformations.SimpleShuffleByWork;
                var initialContext = await initialContextConstructor(_configuration, contextId);
                await initialContext.FullyLoad();
                var trackUriGetter = ReflectionUtils<TrackT>.RetrieveGetterByPropertyName<string>(nameof(SimpleTrack.Uri));
                var trackIsLocalGetter = ReflectionUtils<TrackT>.RetrieveGetterByPropertyName(nameof(FullTrack.IsLocal), track => false);

                IOneTimePlaybackModifier<TrackT,
                    IStaticPlaybackContext<SpotifyItemT, TrackT>,
                    IReorderedPlaybackContext<TrackT, IStaticPlaybackContext<SpotifyItemT, TrackT>>,
                    ITrackReorderingPlaybackTransformation<IStaticPlaybackContext<SpotifyItemT, TrackT>,
                        IReorderedPlaybackContext<TrackT, IStaticPlaybackContext<SpotifyItemT, TrackT>>, TrackT>> modifier =
                    new OneTimeSpotifyPlaybackModifier<TrackT,
                        IStaticPlaybackContext<SpotifyItemT, TrackT>,
                        IReorderedPlaybackContext<TrackT, IStaticPlaybackContext<SpotifyItemT, TrackT>>,
                        ITrackReorderingPlaybackTransformation<IStaticPlaybackContext<SpotifyItemT, TrackT>,
                            IReorderedPlaybackContext<TrackT, IStaticPlaybackContext<SpotifyItemT, TrackT>>, TrackT>>
                    (_configuration, transformation, trackUriGetter, trackIsLocalGetter);

                await modifier.RunOnce(initialContext, GlobalCommandLine.Store.GetOptionValue<bool>(CommandLineOptions.Names.MaintainCurrentlyPlaying));
                Logger.Information("Should be something new playing");
                return true;
            }
            catch (Exception e)
			{
                Logger.Error($"Could not modify current playback as intended because of exception: {e}");
                return false;
			}
        }

        private readonly bool _shouldDefaultToAlbumReordering;
        private readonly SpotifyConfiguration _configuration;
        private readonly SpotifyClient _spotify;
    }
}
