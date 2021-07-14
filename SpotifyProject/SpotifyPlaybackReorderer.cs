﻿using System.Threading.Tasks;
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
using SpotifyProject.SpotifyPlaybackModifier.PlaybackSetters;

namespace SpotifyProject
{
    public class SpotifyPlaybackReorderer : SpotifyAccessorBase
    {

        public SpotifyPlaybackReorderer(SpotifyClient spotify) : base(spotify)
        { }

        public async Task ShuffleUserProvidedContext()
		{
            Task<bool> TryHandleUserInput(string input)
			{
                if (string.Equals(input.Trim(), "current", StringComparison.OrdinalIgnoreCase))
                    return ShuffleCurrentPlayback();
                if (SpotifyDependentUtils.TryParseUriFromLink(input, out var uri))
                    input = uri;
                if (input.StartsWith(SpotifyConstants.SpotifyUriPrefix))
                    return ModifyContext(input);
                if (Enum.TryParse<PlaybackContextType>(input, true, out var contextType) && contextType == PlaybackContextType.AllLikedTracks)
                    return ModifyContext<IOriginalAllLikedTracksPlaybackContext, FullTrack>(contextType, null);
                else
                    return Task.FromResult(false);
			}

            UserInterface.Instance.NotifyUser("Please provide the indicator of the Spotify context to use");
            var userInput = UserInterface.Instance.ReadNextUserInput();
            while (!await TryHandleUserInput(userInput).WithoutContextCapture())
			{
                UserInterface.Instance.NotifyUser("The provided input was not valid. Would you like to try again? If so, you can just provide new input now. " +
					"Otherwise we may default to the current track's album, if that was a startup argument provided");
                var response = UserInterface.Instance.ReadNextUserInput();
                var parsedAffirmation = UserInterface.Instance.ParseAffirmation(response);
                if (parsedAffirmation.HasValue)
				{
                    if (parsedAffirmation.Value)
                        UserInterface.Instance.NotifyUser("Please provide a new Uri");
                    else
                    {
                        if (Settings.Get<bool>(SettingsName.DefaultToAlbumShuffle))
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
            bool result = contextUri != null && await ModifyContext(contextUri).WithoutContextCapture();
            if (result)
                return result;
            if (Settings.Get<bool>(SettingsName.DefaultToAlbumShuffle))
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
                return await ModifyContext<IOriginalAlbumPlaybackContext, SimpleTrack>(PlaybackContextType.Album, album.Id).WithoutContextCapture();            
        }

        private static bool TryParseContextTypeFromUri(string contextUri, out PlaybackContextType contextType, out string contextId)
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
                    result = await ModifyContext<IOriginalAlbumPlaybackContext, SimpleTrack>(contextType, contextId).WithoutContextCapture();
                    break;
				case PlaybackContextType.Artist:
					result = await ModifyContext<IOriginalArtistPlaybackContext, SimpleTrackAndAlbumWrapper>(contextType, contextId).WithoutContextCapture();
					break;
                case PlaybackContextType.Playlist:
                    result = await ModifyContext<IOriginalPlaylistPlaybackContext, FullTrack>(contextType, contextId).WithoutContextCapture();
                    break;
                default:
                    throw new ArgumentException($"Code should not be able to reach here. Please make sure the {nameof(PlaybackContextType)} type's value of " +
                        $"{contextType} is supported in {nameof(SpotifyPlaybackReorderer)}");
            }
            if (!result)
                Logger.Warning($"Failed to modify playback for context type \"{contextType}\" and id \"{contextId}\"");
            return result;
        }

        private async Task<bool> ModifyContext<OriginalContextT, TrackT>(PlaybackContextType contextType, string contextId) where OriginalContextT : IOriginalPlaybackContext, ISpotifyPlaybackContext<TrackT>
        {
            try
            {
                Logger.Information($"Attempting to modify context of type {contextType}{(string.IsNullOrWhiteSpace(contextId) ? "" : $" and with id {contextId}")}");
                if (!PlaybackContexts.TryGetExistingContextConstructorForType<OriginalContextT, TrackT>(contextType, out var initialContextConstructor))
                {
                    Logger.Warning($"There was no initial context constructor found for the context type {contextType}");
                    return false;
                }
                if (!PlaybackTransformations.TryGetTransformation<OriginalContextT, TrackT>(contextType, out var transformations))
                {
                    Logger.Warning($"There was no transformation set found for the context type {contextType}");
                    return false;
                }

                var transformationName = Settings.Get<string>(SettingsName.TransformationName);
                var transformation = transformations.TryGetPropertyByName<IPlaybackTransformation<OriginalContextT, ISpotifyPlaybackContext<TrackT>>>(transformationName, out var namedTransformation)
                    ? namedTransformation : transformations.SimpleShuffleByWork;
                var playbackSetters = new SpotifyUpdaters<TrackT>(SpotifyConfiguration);
                var playbackSetterName = Settings.Get<string>(SettingsName.PlaybackSetterName);
                var playbackSetter = playbackSetters.TryGetPropertyByName<IPlaybackSetter<ISpotifyPlaybackContext<TrackT>, PlaybackStateArgs>>(playbackSetterName, out var namedSetter)
                    ? namedSetter : playbackSetters.QueuePlaybackSetter;

                var initialContext = await initialContextConstructor(SpotifyConfiguration, contextId);

                var modifier = new OneTimeSpotifyPlaybackModifier<OriginalContextT, ISpotifyPlaybackContext<TrackT>>(SpotifyConfiguration, transformation, playbackSetter);

                await modifier.Run(initialContext).WithoutContextCapture();
                return true;
            }
            catch (Exception e)
			{
                Logger.Error($"Could not modify current playback as intended because of exception: {e}");
                return false;
			}
        }
    }
}
