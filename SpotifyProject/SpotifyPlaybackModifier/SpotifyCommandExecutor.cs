using System;
using System.Threading.Tasks;
using ApplicationResources.ApplicationUtils.Parameters;
using ApplicationResources.Logging;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;
using SpotifyAPI.Web;
using SpotifyProject.Configuration;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackModifiers;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackSetters;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;
using SpotifyProject.SpotifyPlaybackModifier.Transformations;
using SpotifyProject.Utils;

namespace SpotifyProject.SpotifyPlaybackModifier
{
	public class SpotifyCommandExecutor : SpotifyAccessorBase
	{
		public SpotifyCommandExecutor(SpotifyClient spotify) : base(spotify)
		{
		}

		public async Task<bool> ModifyContext(string contextUri) =>
            TryParseContextTypeFromUri(contextUri, out var contextType, out var contextId) && await ModifyContext(contextType, contextId);

		private static bool TryParseContextTypeFromUri(string contextUri, out PlaybackContextType contextType, out string contextId)
        {
            contextType = default;
            if (!SpotifyDependentUtils.TryParseSpotifyUri(contextUri, out var typeString, out contextId, out var allParts))
                return false;
            if (allParts.Length > 3)
                Logger.Warning($"The contextUri received from spotify was of an unfamiliar form, but could still be parsed: {contextUri}");
            return Enum.TryParse(typeString, true, out contextType);
        }

        public async Task<bool> ModifyContext(PlaybackContextType contextType, string contextId)
        {
			var result = contextType switch
			{
				PlaybackContextType.Album => await ModifyContext<IOriginalAlbumPlaybackContext, SimpleTrack>(contextType, contextId).WithoutContextCapture(),
				PlaybackContextType.Artist => await ModifyContext<IOriginalArtistPlaybackContext, SimpleTrackAndAlbumWrapper>(contextType, contextId).WithoutContextCapture(),
                PlaybackContextType.Playlist => await ModifyContext<IOriginalPlaylistPlaybackContext, FullTrack>(contextType, contextId).WithoutContextCapture(),
                PlaybackContextType.AllLikedTracks => await ModifyContext<IOriginalAllLikedTracksPlaybackContext, FullTrack>(contextType, null).WithoutContextCapture(),
                _ => throw new NotImplementedException($"Code should not be able to reach here. Please make sure the {nameof(PlaybackContextType)} type's value of " +
                                                       $"{contextType} is supported in {nameof(SpotifyPlaybackReorderer)}"),
			};
			if (!result)
                Logger.Warning($"Failed to modify playback for context type \"{contextType}\" and id \"{contextId}\"");
            return result;
        }

        private async Task<bool> ModifyContext<OriginalContextT, TrackT>(PlaybackContextType contextType, string contextId) where OriginalContextT : IOriginalPlaybackContext, ISpotifyPlaybackContext<TrackT>
        {
            try
            {
                Logger.Information($"Attempting to modify context of type {contextType}{(string.IsNullOrWhiteSpace(contextId) ? "" : $" and with id {contextId}")}");
                if (!PlaybackContextConstructors.TryGetExistingContextConstructorForType<OriginalContextT, TrackT>(contextType, out var initialContextConstructor))
                {
                    Logger.Warning($"There was no initial context constructor found for the context type {contextType}");
                    return false;
                }
                if (!PlaybackTransformations.TryGetTransformation<OriginalContextT, TrackT>(contextType, out var transformations))
                {
                    Logger.Warning($"There was no transformation set found for the context type {contextType}");
                    return false;
                }

                var transformationName = TaskParameters.Get<string>(SpotifyParameters.TransformationName) ?? nameof(transformations.SimpleShuffleByWork);
                var transformation = transformations.GetPropertyByName<IPlaybackTransformation<OriginalContextT, ISpotifyPlaybackContext<TrackT>>>(transformationName);
                var playbackSetters = new SpotifyUpdaters<TrackT>(SpotifyConfiguration);
                var playbackSetterName = TaskParameters.Get<string>(SpotifyParameters.PlaybackSetterName) ?? nameof(playbackSetters.QueuePlaybackSetter);
                var playbackSetter = playbackSetters.GetPropertyByName<IPlaybackSetter<ISpotifyPlaybackContext<TrackT>, PlaybackStateArgs>>(playbackSetterName);

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
