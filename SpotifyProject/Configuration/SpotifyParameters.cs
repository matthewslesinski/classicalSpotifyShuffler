﻿using System;
using System.Collections.Generic;
using System.Linq;
using ApplicationResources.Setup;
using CustomResources.Utils.GeneralUtils;
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
		DefaultToAlbumShuffle,
		ArtistAlbumIncludeGroups,
		MaintainCurrentlyPlaying,
		TransformationName,
		RetryHandlerName,
		PaginatorName,
		APIConnectorName,
		PlaybackSetterName,
		SaveAsPlaylistName,

	}


	public class SpotifyParametersSpecifications : IEnumExtensionProvider<SpotifyParameters, IParameterSpecification>
	{
		public IReadOnlyDictionary<SpotifyParameters, IParameterSpecification> Specifications { get; } = new Dictionary<SpotifyParameters, IParameterSpecification>
		{
			{ SpotifyParameters.TransformationName,                new StringSettingSpecification() },
			{ SpotifyParameters.RetryHandlerName,                  new StringSettingSpecification() },
			{ SpotifyParameters.PaginatorName,                     new StringSettingSpecification() },
			{ SpotifyParameters.APIConnectorName,                  new StringSettingSpecification() },
			{ SpotifyParameters.PlaybackSetterName,                new StringSettingSpecification() },
			{ SpotifyParameters.SaveAsPlaylistName,                new StringSettingSpecification() },
			{ SpotifyParameters.TokenPath,                         new StringSettingSpecification() },
			{ SpotifyParameters.ClientInfoPath,                    new StringSettingSpecification() },
			{ SpotifyParameters.RedirectUri,                       new StringSettingSpecification() },
			{ SpotifyParameters.DefaultToAlbumShuffle,             new BoolSettingSpecification() },
			{ SpotifyParameters.MaintainCurrentlyPlaying,          new BoolSettingSpecification() },
			{ SpotifyParameters.ArtistAlbumIncludeGroups,          new MultipleStringsSettingSpecification { ValueGetter = values => values.Single().Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() } },
		};
	}

	public class SpotifyParametersCommandLineSpecifications : IEnumExtensionProvider<SpotifyParameters, ICommandLineSpecification>
	{
		public IReadOnlyDictionary<SpotifyParameters, ICommandLineSpecification> Specifications { get; } = new Dictionary<SpotifyParameters, ICommandLineSpecification>
		{
			{ SpotifyParameters.ClientInfoPath,                  new SingleValueOption { Flag = "-c|--clientInfoPath <CLIENT_INFO_PATH>", Desc = "The path (from the project root) for the file with the client id and secret for Spotify access" } },
			{ SpotifyParameters.TokenPath,                       new SingleValueOption { Flag = "-t|--tokenPath <TOKEN_PATH>", Desc = "The path (from the project root) for the file with access and refresh tokens for Spotify access" } },
			{ SpotifyParameters.RedirectUri,                     new SingleValueOption { Flag = "-r|--redirectUri <REDIRECT_URI>", Desc = "The path Spotify should use as a redirect Uri" } },
			{ SpotifyParameters.DefaultToAlbumShuffle,           new NoValueOption     { Flag = "--defaultToAlbumShuffle", Desc = "Provide if shuffling the album should be used as a fallback" } },
			{ SpotifyParameters.ArtistAlbumIncludeGroups,        new SingleValueOption { Flag = "-a|--artistAlbumIncludeGroups", Desc = "The types of albums to include when querying for artists' albums" } },
			{ SpotifyParameters.MaintainCurrentlyPlaying,        new NoValueOption     { Flag = "--maintainCurrentlyPlaying", Desc = "Provide if playing from the current context should keep what's currently playing" } },
			{ SpotifyParameters.TransformationName,              new SingleValueOption { Flag = "--transformation", Desc = "The name of the transformation to be used." } },
			{ SpotifyParameters.PaginatorName,                   new SingleValueOption { Flag = "--paginator", Desc = "The name of the paginator to be used." } },
			{ SpotifyParameters.RetryHandlerName,                new SingleValueOption { Flag = "--retryHandler", Desc = "The name of the retry handler to be used." } },
			{ SpotifyParameters.APIConnectorName,                new SingleValueOption { Flag = "--apiConnector", Desc = "The name of the APIConnector to be used." } },
			{ SpotifyParameters.PlaybackSetterName,              new SingleValueOption { Flag = "--playbackSetter", Desc = "The name of the playback setter to be used." } },
			{ SpotifyParameters.SaveAsPlaylistName,              new SingleValueOption { Flag = "--playlistName", Desc = "The name of the playlist to save the playback in." } },
		};
	}
}