# C2 — Container Diagram

The Container diagram zooms into the GetIPlayer.NET system to show the major deployable / runnable units and their relationships.

```mermaid
C4Container
    title Container Diagram — GetIPlayer.NET

    Person(user, "User", "Searches, downloads, and manages BBC programmes.")

    System_Boundary(system, "GetIPlayer.NET") {
        Container(web, "GetIPlayer.Web", "ASP.NET Core Razor Pages, SignalR, .NET 10", "Web UI for searching, downloading, and managing programmes. Hosts a BackgroundService for async downloads.")
        Container(cli, "GetIPlayer.Cli", "System.CommandLine, .NET 10 Console App", "Command-line interface exposing search, get, pvr, info, history, and prefs commands.")
        Container(app, "GetIPlayer.Application", ".NET 10 Class Library", "Application-layer orchestrators: SearchOrchestrator, DownloadOrchestrator, PvrOrchestrator. Coordinates workflows across infrastructure services.")
        Container(core, "GetIPlayer.Core", ".NET 10 Class Library", "Domain models (Programme, SearchResult, StreamInfo, etc.), interfaces (IProgrammeService, IStreamService, etc.), configuration records, custom exceptions, and enums.")
        Container(infra, "GetIPlayer.Infrastructure", ".NET 10 Class Library", "Concrete implementations: BBC API clients, HTTP services, HLS/DASH downloaders, JSON file persistence, ffmpeg post-processing, file system abstraction.")
    }

    System_Ext(bbcApi, "BBC APIs", "Programme metadata, search, and media streams")
    System_Ext(ffmpeg, "ffmpeg", "Media post-processing")
    System_Ext(fs, "File System", "Cache, history, settings, media output")

    Rel(user, web, "Uses via browser", "HTTP / WebSocket")
    Rel(user, cli, "Uses via terminal", "stdin / stdout")
    Rel(web, app, "Delegates to")
    Rel(cli, app, "Delegates to")
    Rel(app, core, "Depends on (models & interfaces)")
    Rel(app, infra, "Uses via DI (interface implementations)")
    Rel(infra, core, "Implements interfaces from")
    Rel(infra, bbcApi, "Calls", "HTTPS")
    Rel(infra, ffmpeg, "Invokes", "Process exec")
    Rel(infra, fs, "Reads/Writes", "File I/O")
```

## Container Descriptions

### GetIPlayer.Web (`GetIPlayer.Web.csproj`)

| Aspect | Detail |
|--------|--------|
| **Type** | ASP.NET Core Razor Pages application |
| **Pages** | `Index`, `Search`, `Programme`, `Download`, `History/Index`, `Pvr/Index`, `Pvr/Add`, `Settings`, `Error` |
| **Real-time** | `DownloadHub` (SignalR) broadcasts download progress to connected browsers |
| **Background** | `DownloadBackgroundService` reads from a `Channel<DownloadRequest>` queue and processes downloads sequentially |
| **State** | `DownloadTracker` (in-memory `ConcurrentDictionary`) tracks active/completed downloads |

### GetIPlayer.Cli (`GetIPlayer.Cli.csproj`)

| Aspect | Detail |
|--------|--------|
| **Type** | Console application using `System.CommandLine` |
| **Commands** | `search`, `get`, `info`, `pvr`, `history`, `refresh`, `prefs` |
| **DI** | Builds its own `ServiceCollection` with the same `AddGetIPlayerServices()` extension |

### GetIPlayer.Application (`GetIPlayer.Application.csproj`)

| Aspect | Detail |
|--------|--------|
| **Orchestrators** | `SearchOrchestrator`, `DownloadOrchestrator`, `PvrOrchestrator` |
| **Role** | Application-layer use-case coordinators; no direct dependency on concrete infrastructure |

### GetIPlayer.Core (`GetIPlayer.Core.csproj`)

| Aspect | Detail |
|--------|--------|
| **Models** | `Programme`, `SearchResult`, `StreamInfo`, `StreamVersion`, `DownloadResult`, `DownloadProgress`, `HistoryRecord`, `PvrSearch`, `ChannelInfo`, `TagMetadata`, `SubtitleData` |
| **Interfaces** | `IProgrammeService`, `IStreamService`, `IDownloadService`, `ICacheService`, `IHistoryService`, `IPvrService`, `IHttpClientService`, `IHlsDownloader`, `IDashDownloader`, `IPostProcessor`, `ISubtitleService`, `IFileSystem`, `IExternalProcessRunner` |
| **Configuration** | `AppSettings`, `DownloadOptions`, `OutputOptions`, `ProxySettings` |
| **Exceptions** | `GetIPlayerException`, `GeoBlockedException`, `ProgrammeNotFoundException`, `StreamUnavailableException`, `DownloadFailedException`, `ValidationException` |
| **Enums** | `ProgrammeType`, `QualityLevel`, `DownloadStatus`, `StreamProtocol` |

### GetIPlayer.Infrastructure (`GetIPlayer.Infrastructure.csproj`)

| Aspect | Detail |
|--------|--------|
| **BBC** | `BbcProgrammeService`, `BbcStreamService`, `MetadataParser`, `UrlBuilder`, `PidValidator` |
| **HTTP** | `BbcHttpClientService`, `UserAgentProvider`, `ProxyHandler` |
| **Streaming** | `HlsDownloader`, `DashDownloader`, `SegmentDownloader`, `M3u8Parser`, `MpdParser` |
| **Persistence** | `JsonCacheService`, `JsonHistoryService`, `PvrFileService`, `OptionsFileService`, `LockFileService` |
| **Post-processing** | `FfmpegProcessor`, `SubtitleConverter`, `ThumbnailDownloader` |
| **File System** | `SafeFileSystem`, `FileNamingService`, `SafeProcessRunner` |
| **DI** | `ServiceCollectionExtensions.AddGetIPlayerServices()` wires everything up |

## Dependency Direction

```
Web / Cli  →  Application  →  Core (interfaces & models)
                  ↕
             Infrastructure  →  Core (implements interfaces)
```

Both host projects (`Web`, `Cli`) reference `Application` and `Infrastructure`. `Application` and `Infrastructure` both reference `Core`. `Infrastructure` never references `Application`.
