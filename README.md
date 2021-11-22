# Classical Music Spotify Shuffler

## Overview

This project aims to produce an app/application that can manipulate a Spotify user's library and playback queue, specifically with emphasis on support for classical music concepts that are not normally supported in music streaming services. In the process of writing this functionality, I have also created an extensive library of general purpose data structures and other utilities. The project is primarily written in C#. Currently it exists as a desktop application, but I plan to expand it to an app.

### Background

In classical music, many compositions (also referred to as "pieces") consist of multiple movements, frequently with mostly unrelated musical material. These movements, then, each can function as separate, standalone pieces, but add up to an even better listening experience when played one after another. In the world of recordings, these movements are frequently given their own track. However, other than the fact that these tracks are frequently ordered one after another on the same album, the tracks do not have any information indicating that they have a special relationship with the other tracks containing movements from the same piece. That means that when someone listens to these tracks in a way that doesn't preserve album order, such as when listening on shuffle or in a playlist, playback will jump between movements of the pieces in a dissatisfying way. 

### Purpose

The main features of this project aim to solve the above problem. There are multiple approaches that can be used. The fundamental use case is that the user specifies a playback context in Spotify that, presumably, they are currently listening to or they want to listen to, specifically on shuffle. The program then retrieves the tracks in that context, heuristically groups them into pieces, shuffles the pieces while maintaining movement order, and sets the shuffled order as the current playback queue for the user in Spotify. Other use cases include saving the resulting shuffled order as a playlist instead (with the option to also set that playlist as the current playback too) and ordering the tracks in different ways. 

## How to run

### Dependencies

- [SpotifyAPI](https://github.com/morcanie/SpotifyAPI-NET)
    - This is a fork of JohnnyCrazy's [SpotifyAPI-NET](https://github.com/JohnnyCrazy/SpotifyAPI-NET) that I so that I can change some functionality. This provides a helpful framework within which to write code that communicates with the Spotify Api.
    - The dlls from this project are included in this repository, in the [SpotifyAPIBinaries](https://github.com/morcanie/classicalSpotifyShuffler/tree/main/SpotifyAPIBinaries) directory.
- Various Nuget packages including:
    - [CommandLineUtils](https://github.com/natemcmaster/CommandLineUtils)
    - [Microsoft.Extensions.Logging](https://www.nuget.org/packages/Microsoft.Extensions.Logging/)
    - [Json.NET](https://www.newtonsoft.com/json)
    - [NLog](https://nlog-project.org/) and [NLog.Extensions.Logging](https://github.com/NLog/NLog.Extensions.Logging)
- Various Nuget packages only used in tests, with the important ones being:
    - [NUnit](https://nunit.org/)
    - [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet)

### Prerequisites

- A paid Spotify account
- A Client ID and Client Secret

#### Prerequisite Details

Any functionality in this project that communicates with Spotify makes use of the [Spotify API](https://developer.spotify.com/documentation/web-api/). Because this project is still in development, use of the Spotify API by this app requires a Client ID and Client Secret, which are not available to the public. If you would like to try it out, contact me (see below) and I can provide the necessary information. Briefly, though, once running with a Client ID and Secret, the user will be prompted on first use to authenticate with Spotify in a browser. This also means the user must have a Spotify account to proceed. The authentication should return an Authorization Code (last I checked, this was returned in the url as a query parameter). Then, this authorization code can be fed back into the program, and it will be used to retrieve an access token and refresh token, which will be saved to a file and used from then on, so the user does not need to re-authenticate.

### Configuration

The easiest way to use the authorization process above is to specify filepaths as the values for parameters such as ClientInfoPath and TokenPath (The files pointed to should be in JSON format). In general, all of the settings and parameters used to run the program can be supplied using either the command line or xml files. Some of them have default values, others are optional, and others will be requested from the user if not provided. The possible values are the enums in files such as [BasicSettings.cs](https://github.com/morcanie/classicalSpotifyShuffler/blob/main/ApplicationResources/Setup/BasicSettings.cs) and [SpotifyParameters.cs](https://github.com/morcanie/classicalSpotifyShuffler/blob/main/SpotifyProject/Configuration/SpotifyParameters.cs), as well as other files in the same directories as those files. The way to specify values for the settings and parameters through the command line, as well as descriptions of what they're used for, should be defined in the same files, and to provide values via a provided file, pass the file's path to the command line as the value for the flag "--settingsXmlFile", which can be used multiple times. Some files of useful values are added by default, for instance [standardSpotifySettings.xml](https://github.com/morcanie/classicalSpotifyShuffler/blob/main/SettingsTemplates/standardSpotifySettings.xml), but their values can be overridden by passing in additional files or through the command line.

## Project Layout

### High Level Structure

The project is divided into five main directories:
- CustomResources: A library of concepts, data structures, algorithms, extensions, and utilities that have no external dependencies and could be useful in a generic project.
- ApplicationResources: Similarly, a library of utilities that are agnostic to this specific project. However, they are specifically geared towards building an application, and have some external dependencies.
- SpotifyProject: The main set of functionality for this project.
- ApplicationResourcesTests: NUnit tests for functionality in both CustomResources and ApplicationResources.
- SpotifyProjectTests: NUnit tests for functionality in SpotifyProject.

### SpotifyProject Assembly Structure

[Program.cs](https://github.com/morcanie/classicalSpotifyShuffler/blob/main/SpotifyProject/Program.cs) is the main entrypoint for running the application. The execution flow deals with initialization logic (for instance, registering and loading settings and parameters), then progresses through the authentication steps (the logic for this is housed in the Authentication subdirectory), and finally executes the main functionality within the SpotifyPlaybackModifier subdirectory. Within this part of the application, the intended behavior is determined and initiated in the [SpotifyPlaybackReorderer](https://github.com/morcanie/classicalSpotifyShuffler/blob/main/SpotifyProject/SpotifyPlaybackModifier/SpotifyPlaybackReorderer.cs) and [SpotifyCommandExecutor](https://github.com/morcanie/classicalSpotifyShuffler/blob/main/SpotifyProject/SpotifyPlaybackModifier/SpotifyCommandExecutor.cs). 

Because so much of the functionality in the SpotifyPlaybackModifier directory is modular, most of the determination of intended behavior involves determining which implementation of each of the steps in the process is to be used. Those steps consist of an initial PlaybackContext (e.g. an album), a Transformation (e.g. shuffling full compositions) to transform the initial context into a modified one, a PlaybackSetter (e.g. setting the context as the playback queue) to actually send the updated context to the Spotify API, and a PlaybackModifier (e.g. doing the whole process once and then being done) in which to perform this process. Furthermore, some Transformations make use of a TrackLinker to determine how to group tracks into larger works/compositions.

### Notable Design Patterns

One of the main design patterns I experimented with in this project is Trait-Based Programming, specifically making use of [default interface implementations](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-8.0/default-interface-methods) available starting in C# 8.0. Along that line, I tried to abstract out many conceptual implementations into interface methods, such that the interfaces, through default methods, would frequently be the implementers of a particular set of behavior that then could be given to a particular class by having the class implement the interface. Some of the main motivations for taking this approach were to try to make things very modular, to abstract things out where possible, to minimize code duplication, and to see how far I could push the C# language towards the direction of true multiple inheritance.

### Interesting Implementations

Some particularly interesting concepts implemented include the following. Not all of them were necessary to be implemented as thoroughly as I did, but I wanted to have some fun with these:
- The EfficientPlaylistTrackModifier class' functionality. The implementation for saving a context as a (possibly already existing) playlist was heavily constrained by the Spotify API's limitations on what operations could be performed. For large playlists, the possible operations were essentially limited to: adding an array of tracks to the playlist at some index, removing all instances of a set of track ids from the playlist, and moving a consecutive sequence of tracks from one index to another. Also, each operation was limited to at most 100 tracks at a time, and the remove and reorder operations could be performed on a previous version (i.e. snapshot) of the playlist and Spotify's server would figure out how to merge the operation into the most recent version. Under these constraints, I devised an algorithm that uses an [implementation](https://github.com/morcanie/classicalSpotifyShuffler/blob/main/CustomResources/Utils/Algorithms/LongestCommonSubsequence.cs) of the [Longest Increasing Subsequence](https://en.wikipedia.org/wiki/Longest_increasing_subsequence) problem to determine the way to update the playlist that involves the fewest number of requests sent to the API and such that many of them can be sent concurrently.
- Rate Limit Prevention. Spotify's API is guarded with [rate limits](https://developer.spotify.com/documentation/web-api/guides/rate-limits/), tracked per app, that limit the number of requests that get a meaningful response within a given span of time (according to documentation, this is 30 seconds). In practice, I would frequently run up against this rate limit during testing of the application when trying to send requests to the API concurrently. I implemented functionality in [RetryProtectedHttpClient.cs](https://github.com/morcanie/classicalSpotifyShuffler/blob/main/SpotifyProject/SpotifyAdditions/RetryProtectedHttpClient.cs) that tracks how many requests have been sent out in a given time frame and linearizes requests when approaching what it thinks is the rate limit. This involves many complicated pieces that all work together. For instance, the [queue](https://github.com/morcanie/classicalSpotifyShuffler/blob/main/CustomResources/Utils/Concepts/DataStructures/TaskQueue.cs) used for ensuring requests get sent sequentially when enough are out also preserves [ExecutionContext](https://docs.microsoft.com/en-us/dotnet/api/system.threading.executioncontext?view=net-6.0), and still treats requests popped from the queue as part of their original Task execution flow. Also, keeping track of how many requests are out in the given timeframe involves a [custom implementation](https://github.com/morcanie/classicalSpotifyShuffler/blob/main/CustomResources/Utils/Concepts/DataStructures/InternalConcurrentSinglyLinkedQueue.cs) of a concurrent queue, and specific logic to ensure response times are correctly ordered across tasks. Finally, there is logic included to calculate what the expected rate limit is, based on previous response results.
- Settings and task-specific parameters are made accessible through static classes that house singletons that act as dictionaries, specifically [Settings.cs](https://github.com/morcanie/classicalSpotifyShuffler/blob/main/ApplicationResources/Setup/Settings.cs) and [TaskParameters](https://github.com/morcanie/classicalSpotifyShuffler/blob/main/ApplicationResources/ApplicationUtils/Parameters/TaskParameters.cs). The singleton design pattern has some notable drawbacks, though. Specifically, especially when it comes to task-specific parameters, singletons traditionally enforce global scope that would be the same across disparate tasks, possibly running in parallel. This can particularly impact unit testing. To mitigate this, I implemented a [dictionary](https://github.com/morcanie/classicalSpotifyShuffler/blob/main/CustomResources/Utils/Concepts/DataStructures/ScopedConcurrentDictionary.cs) data structure that can use the [AsyncLocal](https://docs.microsoft.com/en-us/dotnet/api/system.threading.asynclocal-1?view=net-5.0) storage mechanism to make sure that any values placed in the dictionary are only visible in the current task's execution flow. This is what backs the TaskParameters implementation.
- [ReflectionUtils](https://github.com/morcanie/classicalSpotifyShuffler/blob/main/CustomResources/Utils/GeneralUtils/ReflectionUtils.cs) provides methods for accessing properties on any object by name, when the specific property is not known at compile time. This can be achieved by pure reflection, of course, but I went further use C#'s [Expressions Library](https://docs.microsoft.com/en-us/dotnet/api/system.linq.expressions?view=net-5.0) to cache methods for retrieving the properties from an object. This way, the inflated performance cost of using reflection is only incurred once, when compiling the Expressions, and from then on calling the properties' getters this way performs similarly to doing a dictionary lookup. 
- [LukesTrackLinker](https://github.com/morcanie/classicalSpotifyShuffler/blob/main/SpotifyProject/SpotifyPlaybackModifier/TrackLinking/LukesTrackLinker.cs) is a TrackLinker implementation that specifically calls into an imported c++ assembly. The purpose of this was because my friend Luke tried to attempt to implement more intelligent track linking with machine learning, and he wrote it in C++. While he, as of time of writing, never got [his implementation](https://github.com/gpane/work-identifier) to the point of usefulness, this implementation allows any external track linker, written in a .NET-compatible language, to be used.

## Future Plans

Additional changes and features I hope to implement in the future:
- Making the program into an app or website with a useful UI. 
- Allowing the user to specify more attributes to determine which tracks to include in playback contexts.
- More intelligent track linking (the process of determining which tracks belong to the same larger work's recording). Currently, this is heuristic based and purely off of the album, track name, and track position within the album. Future enhancements could include things such as making use of additional metadata, using other resources to gather track metadata that Spotify doesn't provide, caching track data locally (e.g. in a database) so that it doesn't need to always be queried and can be enhanced with additional metadata, and using more advanced heuristics or even machine learning to identify track linking better. 
- Additional transformations that reorder playback in more customized ways.

## Credits

- Matthew Slesinski (mslesinski12@gmail.com): project owner
- Luke Pane ([gpane](https://github.com/gpane)): contributor




