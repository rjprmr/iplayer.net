# C3 — Component Diagram

The Component diagram zooms into each container to show the major classes/components and their interactions.

## Application Layer Components

```mermaid
C4Component
    title Component Diagram — GetIPlayer.Application

    Container_Boundary(app, "GetIPlayer.Application") {
        Component(searchOrch, "SearchOrchestrator", "Class", "Coordinates programme search across BBC APIs. Aggregates results for multiple ProgrammeTypes.")
        Component(downloadOrch, "DownloadOrchestrator", "Class, IDownloadService", "Full download workflow: history check → metadata fetch → stream discovery → segment download → post-process → tag → history record.")
        Component(pvrOrch, "PvrOrchestrator", "Class", "Runs all PVR saved searches, refreshes cache, and triggers downloads for matches.")
    }

    Container_Boundary(core, "GetIPlayer.Core — Interfaces") {
        Component(iProgramme, "IProgrammeService", "Interface", "Search and metadata retrieval")
        Component(iStream, "IStreamService", "Interface", "Stream discovery and version resolution")
        Component(iHls, "IHlsDownloader", "Interface", "HLS stream download")
        Component(iDash, "IDashDownloader", "Interface", "DASH stream download")
        Component(iPost, "IPostProcessor", "Interface", "Remux and media post-processing")
        Component(iSub, "ISubtitleService", "Interface", "Subtitle download")
        Component(iHistory, "IHistoryService", "Interface", "Download history persistence")
        Component(iCache, "ICacheService", "Interface", "Programme index cache")
        Component(iPvr, "IPvrService", "Interface", "PVR saved search persistence and execution")
        Component(iDownload, "IDownloadService", "Interface", "Download orchestration contract")
    }

    Rel(searchOrch, iProgramme, "Calls SearchAsync per type")
    Rel(searchOrch, iCache, "Reads cached programmes")
    Rel(downloadOrch, iProgramme, "GetByPidAsync for metadata")
    Rel(downloadOrch, iStream, "GetStreamsAsync for stream URLs")
    Rel(downloadOrch, iHls, "Downloads HLS streams")
    Rel(downloadOrch, iDash, "Downloads DASH streams")
    Rel(downloadOrch, iPost, "Remuxes to MP4/M4A")
    Rel(downloadOrch, iSub, "Downloads subtitles")
    Rel(downloadOrch, iHistory, "Checks/adds history records")
    Rel(pvrOrch, iPvr, "Runs saved searches, adds/deletes entries")
    Rel(pvrOrch, iCache, "Refreshes cache before PVR run")
```

## Infrastructure Layer Components

```mermaid
C4Component
    title Component Diagram — GetIPlayer.Infrastructure

    Container_Boundary(infra, "GetIPlayer.Infrastructure") {

        Component(bbcProg, "BbcProgrammeService", "IProgrammeService", "Fetches programme metadata and search results from BBC JSON APIs. Uses UrlBuilder and MetadataParser.")
        Component(bbcStream, "BbcStreamService", "IStreamService", "Discovers HLS/DASH stream URLs via playlist.json. Resolves versions and quality levels.")
        Component(urlBuilder, "UrlBuilder", "Static", "Constructs BBC API URLs with safe escaping. OWASP A10 compliant.")
        Component(metaParser, "MetadataParser", "Class", "Parses JSON responses from BBC APIs into Programme domain models.")
        Component(pidValidator, "PidValidator", "Static", "Validates BBC PIDs match expected format (alphanumeric, 8+ chars).")

        Component(httpClient, "BbcHttpClientService", "IHttpClientService", "HTTP client with TLS enforcement, URL domain validation, user-agent rotation.")
        Component(uaProvider, "UserAgentProvider", "Class", "Rotates browser-like User-Agent strings.")
        Component(proxyHandler, "ProxyHandler", "Static", "Configures HTTP proxy on HttpClientHandler.")

        Component(hlsDl, "HlsDownloader", "IHlsDownloader", "Parses M3U8 master/media playlists, downloads TS segments, concatenates output.")
        Component(dashDl, "DashDownloader", "IDashDownloader", "Parses MPD manifests, downloads DASH segments, concatenates output.")
        Component(segDl, "SegmentDownloader", "Class", "Shared concurrent segment downloader used by both HLS and DASH downloaders.")
        Component(m3u8, "M3u8Parser", "Static", "Parses HLS M3U8 playlists into variant/segment lists.")
        Component(mpd, "MpdParser", "Static", "Parses MPEG-DASH MPD manifests into adaptation set/segment lists.")

        Component(jsonCache, "JsonCacheService", "ICacheService", "JSON file-based programme listing cache with TTL-based expiry.")
        Component(jsonHist, "JsonHistoryService", "IHistoryService", "JSON file-based download history with file-level locking.")
        Component(pvrFile, "PvrFileService", "IPvrService", "One-JSON-file-per-search PVR persistence. Runs searches against cache and triggers downloads.")
        Component(optionsFile, "OptionsFileService", "Class", "Loads/saves AppSettings from options.json.")
        Component(lockFile, "LockFileService", "Class", "Prevents concurrent get_iplayer instances via lock files.")

        Component(ffmpeg, "FfmpegProcessor", "IPostProcessor", "Invokes ffmpeg via SafeProcessRunner for remux, subtitle merge, audio extraction.")
        Component(subConv, "SubtitleConverter", "Class", "Converts BBC subtitle formats (TTML) to SRT.")
        Component(thumbDl, "ThumbnailDownloader", "Class", "Downloads programme thumbnail images for tagging.")

        Component(safeFs, "SafeFileSystem", "IFileSystem", "Safe file operations with path traversal protection.")
        Component(naming, "FileNamingService", "Class", "Generates output filenames from templates and programme metadata.")
        Component(safeProc, "SafeProcessRunner", "IExternalProcessRunner", "Executes external processes with argument sanitisation. OWASP A03 compliant.")
    }

    Rel(bbcProg, urlBuilder, "Builds URLs")
    Rel(bbcProg, metaParser, "Parses responses")
    Rel(bbcProg, httpClient, "Fetches JSON")
    Rel(bbcProg, pidValidator, "Validates PIDs")
    Rel(bbcStream, urlBuilder, "Builds playlist URLs")
    Rel(bbcStream, httpClient, "Fetches playlist JSON")
    Rel(hlsDl, m3u8, "Parses M3U8")
    Rel(hlsDl, segDl, "Downloads segments")
    Rel(dashDl, mpd, "Parses MPD")
    Rel(dashDl, segDl, "Downloads segments")
    Rel(segDl, httpClient, "HTTP GET segments")
    Rel(ffmpeg, safeProc, "Executes ffmpeg")
    Rel(pvrFile, jsonCache, "Reads cached programmes")
```

## Web Container Components

```mermaid
C4Component
    title Component Diagram — GetIPlayer.Web

    Container_Boundary(web, "GetIPlayer.Web") {
        Component(indexPage, "IndexModel", "Razor Page", "Dashboard: active downloads, recent history, PVR search count.")
        Component(searchPage, "SearchModel", "Razor Page", "Search form with type/channel filters. Queues selected programmes for download.")
        Component(progPage, "ProgrammeModel", "Razor Page", "Displays detailed programme information by PID.")
        Component(downloadPage, "DownloadModel", "Razor Page", "Shows active/completed downloads with real-time progress.")
        Component(historyPage, "History/IndexModel", "Razor Page", "Displays download history records.")
        Component(pvrIndexPage, "Pvr/IndexModel", "Razor Page", "Lists saved PVR searches.")
        Component(pvrAddPage, "Pvr/AddModel", "Razor Page", "Form to create a new PVR saved search.")
        Component(settingsPage, "SettingsModel", "Razor Page", "View/edit application settings.")

        Component(downloadHub, "DownloadHub", "SignalR Hub", "Broadcasts DownloadProgress events to subscribed browser clients.")
        Component(bgService, "DownloadBackgroundService", "BackgroundService", "Reads from Channel<DownloadRequest> queue, invokes IDownloadService, reports progress via SignalR.")
        Component(tracker, "DownloadTracker", "Singleton", "In-memory ConcurrentDictionary tracking active and recently completed downloads.")
        Component(dlRequest, "DownloadRequest", "Record", "Queued download message: Id, Target PID, ConnectionId, Force, Subtitles, Overwrite.")
    }

    Container_Boundary(appLayer, "GetIPlayer.Application") {
        Component(searchOrch2, "SearchOrchestrator", "Orchestrator", "")
        Component(downloadOrch2, "DownloadOrchestrator", "IDownloadService", "")
        Component(pvrOrch2, "PvrOrchestrator", "Orchestrator", "")
    }

    Rel(searchPage, searchOrch2, "SearchAsync")
    Rel(searchPage, bgService, "Queues DownloadRequest via Channel")
    Rel(indexPage, pvrOrch2, "ListSearchesAsync")
    Rel(bgService, downloadOrch2, "DownloadAsync per queued request")
    Rel(bgService, downloadHub, "SendAsync (progress updates)")
    Rel(bgService, tracker, "UpdateProgress / MarkCompleted")
    Rel(downloadPage, tracker, "GetActiveDownloads / GetRecentCompleted")
```

## CLI Container Components

```mermaid
C4Component
    title Component Diagram — GetIPlayer.Cli

    Container_Boundary(cli, "GetIPlayer.Cli") {
        Component(searchCmd, "SearchCommand", "System.CommandLine", "search <term> [--type] [--channel]")
        Component(getCmd, "GetCommand", "System.CommandLine", "get <targets...> [--force] [--subtitles] [--overwrite]")
        Component(infoCmd, "InfoCommand", "System.CommandLine", "info <pid>")
        Component(pvrCmd, "PvrCommand", "System.CommandLine", "pvr [add|delete|list|run]")
        Component(historyCmd, "HistoryCommand", "System.CommandLine", "history [list|clear|purge]")
        Component(refreshCmd, "RefreshCommand", "System.CommandLine", "refresh [--type]")
        Component(prefsCmd, "PrefsCommand", "System.CommandLine", "prefs [get|set|list]")
    }

    Container_Boundary(appLayer2, "GetIPlayer.Application") {
        Component(searchOrch3, "SearchOrchestrator", "", "")
        Component(downloadOrch3, "DownloadOrchestrator", "IDownloadService", "")
        Component(pvrOrch3, "PvrOrchestrator", "", "")
    }

    Rel(searchCmd, searchOrch3, "SearchAsync")
    Rel(getCmd, downloadOrch3, "DownloadAsync / DownloadByIndexAsync / DownloadByUrlAsync")
    Rel(pvrCmd, pvrOrch3, "RunAllAsync / AddSearchAsync / DeleteSearchAsync / ListSearchesAsync")
    Rel(refreshCmd, searchOrch3, "RefreshCacheAsync")
```
