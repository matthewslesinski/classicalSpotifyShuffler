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
using System.Threading;
using CustomResources.Utils.Concepts.DataStructures;

namespace SpotifyProjectTests.SpotifyApiTests
{
	public class SpotifyProjectTestBase : UnitTestBase
	{
		private static readonly AsyncLockProvider _lock = new();
		private static readonly MutableReference<bool> _isLoaded = new(false);

		[OneTimeSetUp]
		public static async Task OneTimeSetUp__SpotifyProjectTestBase()
		{
			var settingsFiles = new[] { GeneralConstants.StandardSpotifyUnitTestSettingsFile, GeneralConstants.StandardConfigurationSettingsFile, GeneralConstants.StandardSpotifySettingsFile };
			await Utils.LoadOnceBlockingAsync(_isLoaded, _lock, async (_) =>
			{
				Settings.RegisterSettings<SpotifySettings>();
				TaskParameters.RegisterParameters<SpotifyParameters>();
				await LoadSettingsFiles(true, settingsFiles).WithoutContextCapture();
				await Settings.Load().WithoutContextCapture();
				await LoadSettingsFiles(false, ApplicationResources.Utils.GeneralUtils.GetAbsoluteCombinedPath(Settings.Get<string>(BasicSettings.ProjectRootDirectory), Settings.Get<string>(SpotifySettings.PersonalDataDirectory), GeneralConstants.SuggestedAuthorizationSettingsFile)).WithoutContextCapture();
			}).WithoutContextCapture();
		}
	}

	public class SpotifyTestBase : SpotifyProjectTestBase
	{
		private static readonly AsyncLockProvider _lock = new();
		private static readonly MutableReference<bool> _isLoaded = new(false);
		private static ISpotifyAccessor _globalSpotifyAccessor;
		protected static ISpotifyAccessor SpotifyAccessor => _globalSpotifyAccessor;

		[OneTimeSetUp]
		public static Task OneTimeSetUp__SpotifyTestBase()
		{
			return Utils.LoadOnceBlockingAsync(_isLoaded, _lock, async (_) =>
			{
				Logger.Information("Loading Spotify Configuration for tests");
				var authenticator = new SpotifyTestAccountAuthenticator();
				var spotifyProvider = new StandardSpotifyProvider(authenticator);
				await spotifyProvider.InitializeAsync().WithoutContextCapture();
				await authenticator.Authenticate(CancellationToken.None).WithoutContextCapture();
				_globalSpotifyAccessor = new SpotifyAccessorBase(spotifyProvider.Client);
			});
		}

		protected enum SampleAlbums
		{
			ShostakovichQuartets,
			BeethovenPianoSonatasAndConcerti,
			BrahmsSymphonies,
			BachKeyboardWorks,
			HilaryHahnIn27Pieces,
			BachCantatas,
			BrahmsSextets,
			EnlishMusicForStrings,
			MustTheDevilHaveAllTheGoodTunes,
			KabalevskyCelloConcertos,
			SibeliusAndNielsenViolinConcertos,
			ProkofievSinfoniaConcertanteAndTchaikovskyRococo,
			VeressTrioAndBartokPianoQuintet,
			CarolineShawOrange,
			NielsenCompleteSymphonies,
		}

		protected enum SampleArtists
		{
			YannickNezetSeguin,
			PhiladelphiaOrchestra,
			HilaryHahn,
			BelceaQuartet
		}

		protected enum SamplePlaylists
		{
			ImportedFromYoutube,
			Brahms
		}

		protected static IReadOnlyDictionary<SampleAlbums, string> SampleAlbumUris => _sampleAlbumUris;
		private static readonly Dictionary<SampleAlbums, string> _sampleAlbumUris = new Dictionary<SampleAlbums, string>
		{
			{ SampleAlbums.ShostakovichQuartets, "spotify:album:46ZSRWpa4VTsGWaPA1AxPy" },
			{ SampleAlbums.BeethovenPianoSonatasAndConcerti, "spotify:album:62VlldLNKK8OGw8vbyIFED" },
			{ SampleAlbums.BrahmsSymphonies, "spotify:album:0kJBUtCkSBYRyc8Jiyyecz" },
			{ SampleAlbums.BachKeyboardWorks, "spotify:album:1FfjKB0aGdGU52uQOuTA6I" },
			{ SampleAlbums.HilaryHahnIn27Pieces, "spotify:album:7GiMQKT1Twq3MOVAGQekF7" },
			{ SampleAlbums.BrahmsSextets, "spotify:album:4Kh7gIrJVphkM0XH1NCBK5" },
			{ SampleAlbums.EnlishMusicForStrings, "spotify:album:61B2Rro2y5Wz8zHofmdpLO" },
			{ SampleAlbums.KabalevskyCelloConcertos, "spotify:album:1PnevN3lMU2pX0T6Dekx2a" },
			{ SampleAlbums.SibeliusAndNielsenViolinConcertos, "spotify:album:1DEQiy1FMxQltnFRMFyWC3" },
			{ SampleAlbums.NielsenCompleteSymphonies, "spotify:album:1dOO94iC2dBaBYMUKx7k4K" },
			{ SampleAlbums.MustTheDevilHaveAllTheGoodTunes, "spotify:album:6CymPaJZK7ZTz79sbCrqnb" },
			{ SampleAlbums.ProkofievSinfoniaConcertanteAndTchaikovskyRococo, "spotify:album:3kKbCgSzqhIaYaIJDQHncg" },
			{ SampleAlbums.VeressTrioAndBartokPianoQuintet, "spotify:album:2jl8aMvxO692tb8Jzkcrvm" },
			{ SampleAlbums.CarolineShawOrange, "spotify:album:5d0tz2baP5WGhMzZvONcgU" },
			{ SampleAlbums.BachCantatas, "spotify:album:46kDQc1UFEKI51yvavxdMm" }
		};

		protected static IReadOnlyDictionary<SampleAlbums, string> SampleAlbumIds => _sampleAlbumUris
			.SelectAsDictionary<SampleAlbums, string, Dictionary<SampleAlbums, string>>(
				valueSelector: contextUri => SpotifyDependentUtils.TryParseSpotifyUri(contextUri, out _, out var parsedId, out _) ? parsedId : null);

		protected static IReadOnlyDictionary<SampleArtists, string> SampleArtistUris => _sampleArtistUris;
		private static readonly Dictionary<SampleArtists, string> _sampleArtistUris = new Dictionary<SampleArtists, string>
		{
			{ SampleArtists.YannickNezetSeguin, "spotify:artist:5ZGyCOrODWwaVtLSDjayl5" },
			{ SampleArtists.PhiladelphiaOrchestra, "spotify:artist:6tdexW8bZTG8NgOFUCYQn1" },
			{ SampleArtists.HilaryHahn, "spotify:artist:5JdT0LYJdlPbTC58p60WTX" },
			{ SampleArtists.BelceaQuartet, "spotify:artist:3w7DwDLRBfI6SfZnLVj7AB" }
		};

		protected static IReadOnlyDictionary<SampleArtists, string> SampleArtistIds => _sampleArtistUris
			.SelectAsDictionary<SampleArtists, string, Dictionary<SampleArtists, string>>(
				valueSelector: contextUri => SpotifyDependentUtils.TryParseSpotifyUri(contextUri, out _, out var parsedId, out _) ? parsedId : null);

		protected static IReadOnlyDictionary<SamplePlaylists, string> SamplePlaylistUris => _samplePlaylistUris;
		private static readonly Dictionary<SamplePlaylists, string> _samplePlaylistUris = new Dictionary<SamplePlaylists, string>
		{
			{ SamplePlaylists.ImportedFromYoutube, "spotify:playlist:3EINXMmb4xyH0jLx8ZHgiC" },
			{ SamplePlaylists.Brahms, "spotify:playlist:1UntiT6SRaIY0nMa5LZ8an" },
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
