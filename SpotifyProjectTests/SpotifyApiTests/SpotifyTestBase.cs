using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using SpotifyProject;
using SpotifyProject.Authentication;
using SpotifyProject.Setup;
using SpotifyProject.SpotifyPlaybackModifier;
using SpotifyProject.Utils;
using System.Linq;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;

namespace SpotifyProjectTests.SpotifyApiTests
{
	public class SpotifyTestBase : UnitTestBase
	{
		private readonly static object _lock = new object();
		private static bool _isLoaded = false;
		private static ISpotifyAccessor _globalSpotifyAccessor;
		protected static ISpotifyAccessor SpotifyAccessor => _globalSpotifyAccessor;

		[OneTimeSetUp]
		public async Task OneTimeSetUp__SpotifyTestBase()
		{
			string authorizationSettingsFileName = Constants.SuggestedAuthorizationSettingsFile;
			await Utils.LoadOnceAsync(() => _isLoaded, isLoaded => _isLoaded = isLoaded, _lock, async () =>
			{
				if (File.Exists(authorizationSettingsFileName))
					Settings.RegisterProvider(new XmlSettingsProvider(authorizationSettingsFileName, SettingsName.ClientInfoPath, SettingsName.TokenPath, SettingsName.RedirectUri));
				else
					throw new FileNotFoundException($"In order to run unit tests, you must provide authorization settings in a file located at {authorizationSettingsFileName}");
				if (File.Exists(Constants.StandardSettingsFile))
					Settings.RegisterProvider(new XmlSettingsProvider(Constants.StandardSettingsFile));
				else
					throw new FileNotFoundException($"In order to run unit tests, you must provide general settings in a file located at {Constants.StandardSettingsFile}");
				Settings.Load();
				Logger.Information("Loading Spotify Configuration for tests");
				var client = await Authenticators.Authenticate(Authenticators.AuthorizationCodeAuthenticator);
				_globalSpotifyAccessor = new SpotifyAccessorBase(client);
			});
		}

		protected enum SampleAlbums
		{
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

		protected static readonly IReadOnlyDictionary<SampleAlbums, string> SampleAlbumUris = new Dictionary<SampleAlbums, string>
		{
			{ SampleAlbums.BeethovenPianoSonatasAndConcerti, "spotify:album:62VlldLNKK8OGw8vbyIFED" },
			{ SampleAlbums.BrahmsSymphonies, "spotify:album:0kJBUtCkSBYRyc8Jiyyecz" },
			{ SampleAlbums.BachKeyboardWorks, "spotify:album:1FfjKB0aGdGU52uQOuTA6I" },
			{ SampleAlbums.HilaryHahnIn27Pieces, "spotify:album:7GiMQKT1Twq3MOVAGQekF7" }
		};

		protected static readonly IReadOnlyDictionary<SampleArtists, string> SampleArtistUris = new Dictionary<SampleArtists, string>
		{
			{ SampleArtists.YannickNezetSeguin, "spotify:artist:5ZGyCOrODWwaVtLSDjayl5" },
			{ SampleArtists.PhiladelphiaOrchestra, "spotify:artist:6tdexW8bZTG8NgOFUCYQn1" },
			{ SampleArtists.HilaryHahn, "spotify:artist:5JdT0LYJdlPbTC58p60WTX" }
		};

		protected string GetPlaylistNameForTest(string testName) => $"TestPlaylist_{GetType().Name}_{testName}";

		protected static string TurnTracksIntoString(IEnumerable<ITrackLinkingInfo> tracks) => TurnUrisIntoString(tracks.Select(track => (track.Uri, track.AlbumName, track.AlbumIndex.discNumber, track.AlbumIndex.trackNumber, track.Name)));

		protected static string TurnUrisIntoString(IEnumerable<string> uris, Func<string, string> trackNameGetter, Func<string, (int discNumber, int trackNumber)> albumIndexGetter, Func<string, string> albumNameGetter) =>
			TurnUrisIntoString(uris.Zip(uris.Select(albumNameGetter), uris.Select(uri => albumIndexGetter(uri).discNumber), uris.Select(uri => albumIndexGetter(uri).trackNumber), uris.Select(trackNameGetter)));

		protected static string TurnUrisIntoString(IEnumerable<(string uri, string albumName, int discNumber, int trackNumber, string trackName)> trackInfos) =>
			$"{string.Join("\n", trackInfos.Select(info => $"\t{info.ToDescriptiveString()}"))}";
	}
}
