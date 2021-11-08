using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using SpotifyProject.Authentication;
using ApplicationResources.Setup;
using SpotifyProject.SpotifyPlaybackModifier;
using CustomResources.Utils.GeneralUtils;
using CustomResources.Utils.Extensions;
using System.Linq;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;
using ApplicationResources.Logging;
using ApplicationResourcesTests;
using SpotifyProject.Configuration;
using SpotifyProject.SpotifyUtils;
using System.IO;
using ApplicationResources.ApplicationUtils.Parameters;
using SpotifyProject.Utils;

namespace SpotifyProjectTests.SpotifyApiTests
{
	public class SpotifyProjectTestBase : UnitTestBase
	{
		private readonly static object _lock = new object();
		private static bool _isLoaded = false;

		[OneTimeSetUp]
		public static void OneTimeSetUp__SpotifyProjectTestBase()
		{
			var settingsFiles = new[] { GeneralConstants.StandardSpotifyUnitTestSettingsFile, GeneralConstants.StandardSpotifySettingsFile };
			Utils.LoadOnce(ref _isLoaded, _lock, () =>
			{
				Settings.RegisterSettings<SpotifySettings>();
				TaskParameters.RegisterParameters<SpotifyParameters>();
				LoadSettingsFiles(true, GeneralConstants.StandardSpotifyUnitTestSettingsFile, GeneralConstants.StandardSpotifySettingsFile);
				Settings.Load();
				LoadSettingsFiles(false, Path.Combine(Settings.Get<string>(BasicSettings.ProjectRootDirectory), GeneralConstants.SuggestedAuthorizationSettingsFile));
			});
		}
	}

	public class SpotifyTestBase : SpotifyProjectTestBase
	{
		private readonly static object _lock = new object();
		private static bool _isLoaded = false;
		private static ISpotifyAccessor _globalSpotifyAccessor;
		protected static ISpotifyAccessor SpotifyAccessor => _globalSpotifyAccessor;

		[OneTimeSetUp]
		public static async Task OneTimeSetUp__SpotifyTestBase()
		{
			var settingsFiles = new[] { GeneralConstants.StandardSpotifyUnitTestSettingsFile, GeneralConstants.StandardSpotifySettingsFile };
			await Utils.LoadOnceAsync(() => _isLoaded, isLoaded => _isLoaded = isLoaded, _lock, async () =>
			{
				Logger.Information("Loading Spotify Configuration for tests");
				var client = await Authenticators.Authenticate(Authenticators.AuthorizationCodeAuthenticator);
				_globalSpotifyAccessor = new SpotifyAccessorBase(client);
			});
		}

		protected enum SampleAlbums
		{
			ShostakovichQuartets,
			BeethovenPianoSonatasAndConcerti,
			BrahmsSymphonies,
			BachKeyboardWorks,
			HilaryHahnIn27Pieces
		}

		protected enum SampleArtists
		{
			YannickNezetSeguin,
			PhiladelphiaOrchestra,
			HilaryHahn
		}

		protected enum SamplePlaylists
		{
			ImportedFromYoutube
		}

		protected static IReadOnlyDictionary<SampleAlbums, string> SampleAlbumUris => _sampleAlbumUris;
		private static readonly Dictionary<SampleAlbums, string> _sampleAlbumUris = new Dictionary<SampleAlbums, string>
		{
			{ SampleAlbums.ShostakovichQuartets, "spotify:album:46ZSRWpa4VTsGWaPA1AxPy" },
			{ SampleAlbums.BeethovenPianoSonatasAndConcerti, "spotify:album:62VlldLNKK8OGw8vbyIFED" },
			{ SampleAlbums.BrahmsSymphonies, "spotify:album:0kJBUtCkSBYRyc8Jiyyecz" },
			{ SampleAlbums.BachKeyboardWorks, "spotify:album:1FfjKB0aGdGU52uQOuTA6I" },
			{ SampleAlbums.HilaryHahnIn27Pieces, "spotify:album:7GiMQKT1Twq3MOVAGQekF7" }
		};

		protected static IReadOnlyDictionary<SampleAlbums, string> SampleAlbumIds => _sampleAlbumUris
			.SelectAsDictionary<SampleAlbums, string, Dictionary<SampleAlbums, string>>(
				valueSelector: contextUri => SpotifyDependentUtils.TryParseSpotifyUri(contextUri, out _, out var parsedId, out _) ? parsedId : null);

		protected static IReadOnlyDictionary<SampleArtists, string> SampleArtistUris => _sampleArtistUris;
		private static readonly Dictionary<SampleArtists, string> _sampleArtistUris = new Dictionary<SampleArtists, string>
		{
			{ SampleArtists.YannickNezetSeguin, "spotify:artist:5ZGyCOrODWwaVtLSDjayl5" },
			{ SampleArtists.PhiladelphiaOrchestra, "spotify:artist:6tdexW8bZTG8NgOFUCYQn1" },
			{ SampleArtists.HilaryHahn, "spotify:artist:5JdT0LYJdlPbTC58p60WTX" }
		};

		protected static IReadOnlyDictionary<SampleArtists, string> SampleArtistIds => _sampleArtistUris
			.SelectAsDictionary<SampleArtists, string, Dictionary<SampleArtists, string>>(
				valueSelector: contextUri => SpotifyDependentUtils.TryParseSpotifyUri(contextUri, out _, out var parsedId, out _) ? parsedId : null);

		protected static IReadOnlyDictionary<SamplePlaylists, string> SamplePlaylistUris => _samplePlaylistUris;
		private static readonly Dictionary<SamplePlaylists, string> _samplePlaylistUris = new Dictionary<SamplePlaylists, string>
		{
			{ SamplePlaylists.ImportedFromYoutube, "spotify:playlist:3EINXMmb4xyH0jLx8ZHgiC" },
		};

		protected static IReadOnlyDictionary<SamplePlaylists, string> SamplePlaylistIds => _samplePlaylistUris
			.SelectAsDictionary<SamplePlaylists, string, Dictionary<SamplePlaylists, string>>(
				valueSelector: contextUri => SpotifyDependentUtils.TryParseSpotifyUri(contextUri, out _, out var parsedId, out _) ? parsedId : null);


		protected string GetPlaylistNameForTest(string testName) => $"TestPlaylist_{GetType().Name}_{testName}";

		protected static string TurnTracksIntoString(IEnumerable<ITrackLinkingInfo> tracks) => TurnUrisIntoString(tracks.Select(track => (track.Uri, track.AlbumName, track.AlbumIndex.discNumber, track.AlbumIndex.trackNumber, track.Name)));

		protected static string TurnUrisIntoString(IEnumerable<string> uris, Func<string, string> trackNameGetter, Func<string, (int discNumber, int trackNumber)> albumIndexGetter, Func<string, string> albumNameGetter) =>
			TurnUrisIntoString(uris.Zip(uris.Select(albumNameGetter), uris.Select(uri => albumIndexGetter(uri).discNumber), uris.Select(uri => albumIndexGetter(uri).trackNumber), uris.Select(trackNameGetter)));

		protected static string TurnUrisIntoString(IEnumerable<(string uri, string albumName, int discNumber, int trackNumber, string trackName)> trackInfos) =>
			$"{string.Join("\n", trackInfos.Select(info => $"\t{info.ToDescriptiveString()}"))}";
	}
}
