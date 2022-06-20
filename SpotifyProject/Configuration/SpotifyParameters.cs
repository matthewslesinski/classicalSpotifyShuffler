using System;
using System.Collections.Generic;
using System.Linq;
using ApplicationResources.Services;
using ApplicationResources.Setup;
using CustomResources.Utils.GeneralUtils;
using SpotifyAPI.Web;
using SpotifyProject.Utils;
using static ApplicationResources.Setup.CommandLineSettingsProvider;

namespace SpotifyProject.Configuration
{

	[EnumExtensionProvider(typeof(SpotifyParametersSpecifications))]
	[EnumExtensionProvider(typeof(SpotifyParametersCommandLineSpecifications))]
	public enum SpotifyParameters
	{
		ClientInfoPath,
		TokenPath,
		RedirectUri,
		AuthenticationType,
		DefaultToAlbumShuffle,
		ArtistAlbumIncludeGroups,
		MaintainCurrentlyPlaying,
		TransformationName,
		RetryHandlerName,
		HTTPClientName,
		PaginatorName,
		APIConnectorName,
		PlaybackSetterName,
		SaveAsPlaylistName,
		PlaylistRequestBatchSize,
		SerializeOperations,
		NumberOfRetriesForServerError,
		MaximumBatchSizeToReplaceInPlaylist,
		HTTPLoggerCharacterLimit,
	}


	public class SpotifyParametersSpecifications : IEnumExtensionProvider<SpotifyParameters, IParameterSpecification>
	{
		public IReadOnlyDictionary<SpotifyParameters, IParameterSpecification> Specifications { get; } = new Dictionary<SpotifyParameters, IParameterSpecification>
		{
			{ SpotifyParameters.TransformationName,						new StringSettingSpecification() },
			{ SpotifyParameters.RetryHandlerName,                       new StringSettingSpecification() },
			{ SpotifyParameters.HTTPClientName,                         new StringSettingSpecification() },
			{ SpotifyParameters.PaginatorName,							new StringSettingSpecification() },
			{ SpotifyParameters.APIConnectorName,						new StringSettingSpecification() },
			{ SpotifyParameters.PlaybackSetterName,						new StringSettingSpecification() },
			{ SpotifyParameters.SaveAsPlaylistName,			            new StringSettingSpecification() },
			{ SpotifyParameters.TokenPath,								new StringSettingSpecification() },
			{ SpotifyParameters.ClientInfoPath,							new StringSettingSpecification() },
			{ SpotifyParameters.RedirectUri,							new StringSettingSpecification() },
			{ SpotifyParameters.DefaultToAlbumShuffle,					new BoolSettingSpecification() },
			{ SpotifyParameters.MaintainCurrentlyPlaying,				new BoolSettingSpecification() },
			{ SpotifyParameters.SerializeOperations,					new BoolSettingSpecification() },
			{ SpotifyParameters.HTTPLoggerCharacterLimit,				new NullableConvertibleSettingSpecification<int> { ValueGetter = values => int.TryParse(values.Single(), out var characterLimit) && characterLimit > 0 ? characterLimit : null, Default = 1000 } },
			{ SpotifyParameters.NumberOfRetriesForServerError,			new ConvertibleSettingSpecification<int> { Default = 10, Validator = v => v > 0 } },
			{ SpotifyParameters.MaximumBatchSizeToReplaceInPlaylist,    new ConvertibleSettingSpecification<int> { Default = (int) (SpotifyConstants.PlaylistRequestBatchSize / 2.5), Validator = v => v >= 0 } },
			{ SpotifyParameters.PlaylistRequestBatchSize,				new ConvertibleSettingSpecification<int> { Default = SpotifyConstants.PlaylistRequestBatchSize, Validator = v => v > 0 } },
			{ SpotifyParameters.AuthenticationType,						new EnumSettingSpecification<AuthenticationType> { IsRequired = true } },
			{ SpotifyParameters.ArtistAlbumIncludeGroups,               new EnumSettingSpecification<ArtistsAlbumsRequest.IncludeGroups> { ValueGetter = values => values.Single()
																			.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(enumName => Enum.Parse<ArtistsAlbumsRequest.IncludeGroups>(enumName, true))
																			.Aggregate((ArtistsAlbumsRequest.IncludeGroups)0, (group1, group2) => group1 | group2) } },
		};
	}

	public class SpotifyParametersCommandLineSpecifications : IEnumExtensionProvider<SpotifyParameters, ICommandLineSpecification>
	{
		public IReadOnlyDictionary<SpotifyParameters, ICommandLineSpecification> Specifications { get; } = new Dictionary<SpotifyParameters, ICommandLineSpecification>
		{
			{ SpotifyParameters.ClientInfoPath,							new SingleValueOption { Flag = "-c|--clientInfoPath <CLIENT_INFO_PATH>", Desc = "The path (from the project root) for the file with the client id and secret for Spotify access" } },
			{ SpotifyParameters.TokenPath,								new SingleValueOption { Flag = "-t|--tokenPath <TOKEN_PATH>", Desc = "The path (from the project root) for the file with access and refresh tokens for Spotify access" } },
			{ SpotifyParameters.RedirectUri,                            new SingleValueOption { Flag = "-r|--redirectUri <REDIRECT_URI>", Desc = "The path Spotify should use as a redirect Uri" } },
			{ SpotifyParameters.AuthenticationType,                     new SingleValueOption { Flag = "-u|--authType <AUTH_TYPE>", Desc = "The type of authentication to use" } },
			{ SpotifyParameters.DefaultToAlbumShuffle,					new NoValueOption     { Flag = "--defaultToAlbumShuffle", Desc = "Provide if shuffling the album should be used as a fallback" } },
			{ SpotifyParameters.ArtistAlbumIncludeGroups,				new SingleValueOption { Flag = "-a|--artistAlbumIncludeGroups", Desc = "The types of albums to include when querying for artists' albums" } },
			{ SpotifyParameters.MaintainCurrentlyPlaying,				new NoValueOption     { Flag = "--maintainCurrentlyPlaying", Desc = "Provide if playing from the current context should keep what's currently playing" } },
			{ SpotifyParameters.SerializeOperations,					new NoValueOption     { Flag = "--serializeOperations", Desc = "Makes operations sent to spotify happen in a sequential manner as opposed to in parallel" } },
			{ SpotifyParameters.TransformationName,						new SingleValueOption { Flag = "--transformation", Desc = "The name of the transformation to be used." } },
			{ SpotifyParameters.PaginatorName,							new SingleValueOption { Flag = "--paginator", Desc = "The name of the paginator to be used." } },
			{ SpotifyParameters.RetryHandlerName,                       new SingleValueOption { Flag = "--retryHandler", Desc = "The name of the retry handler to be used." } },
			{ SpotifyParameters.HTTPClientName,                         new SingleValueOption { Flag = "--httpClient", Desc = "The name of the http client to be used." } },
			{ SpotifyParameters.APIConnectorName,						new SingleValueOption { Flag = "--apiConnector", Desc = "The name of the APIConnector to be used." } },
			{ SpotifyParameters.PlaybackSetterName,						new SingleValueOption { Flag = "--playbackSetter", Desc = "The name of the playback setter to be used." } },
			{ SpotifyParameters.SaveAsPlaylistName,						new SingleValueOption { Flag = "--playlistName", Desc = "The name of the playlist to save the playback in." } },
			{ SpotifyParameters.PlaylistRequestBatchSize,				new SingleValueOption { Flag = "--playlistRequestBatchSize", Desc = "The max number of tracks to include in each playlist modification request" } },
			{ SpotifyParameters.NumberOfRetriesForServerError,          new SingleValueOption { Flag = "--numberOfRetriesForServerError", Desc = "The number of times to retry a request when the server returns an error" } },
			{ SpotifyParameters.MaximumBatchSizeToReplaceInPlaylist,    new SingleValueOption { Flag = "--maximumBatchSizeToReplaceInPlaylist", Desc = "The maximum size of sub arrays of a playlist to remove and add instead of reordering in playlist operations" } },
			{ SpotifyParameters.HTTPLoggerCharacterLimit,				new SingleValueOption { Flag = "--httpLoggerCharacterLimit", Desc = "The max number of characters to print per line from the http logger." } },
		};
	}
}
