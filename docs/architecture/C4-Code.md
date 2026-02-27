# C4 — Code-Level Diagrams

This level zooms into key classes and their relationships for the most important workflows.

## Core Domain Model

```mermaid
classDiagram
    class Programme {
        +string Pid
        +ProgrammeType Type
        +string Name
        +string Episode
        +string Description
        +string Channel
        +string ThumbnailUrl
        +string WebUrl
        +TimeSpan Duration
        +DateTimeOffset? FirstBroadcast
        +DateTimeOffset? AvailableFrom
        +DateTimeOffset? ExpiresAt
        +IReadOnlyList~StreamVersion~ Versions
        +int Index
    }

    class SearchResult {
        +IReadOnlyList~Programme~ Programmes
        +int TotalCount
        +int Page
        +int PageSize
    }

    class StreamInfo {
        +Uri StreamUrl
        +QualityLevel Quality
        +int Bitrate
        +StreamProtocol Protocol
        +string Mode
    }

    class StreamVersion {
        +string Name
        +IReadOnlyList~StreamInfo~ Streams
    }

    class DownloadResult {
        +string Pid
        +DownloadStatus Status
        +string? FilePath
        +long BytesDownloaded
        +string? ErrorMessage
    }

    class DownloadProgress {
        +string Pid
        +string Phase
        +double? PercentComplete
        +long BytesDownloaded
        +long TotalBytes
        +double BytesPerSecond
        +TimeSpan? EstimatedTimeRemaining
    }

    class HistoryRecord {
        +string Pid
        +string Name
        +string Episode
        +DateTimeOffset DownloadedAt
    }

    class PvrSearch {
        +string Name
        +string SearchTerm
        +bool IsEnabled
    }

    class ChannelInfo {
        +string Id
        +string Name
    }

    SearchResult --> Programme : contains *
    Programme --> StreamVersion : has *
    StreamVersion --> StreamInfo : has *
```

## Core Interfaces

```mermaid
classDiagram
    class IProgrammeService {
        <<interface>>
        +SearchAsync(searchTerm, types, channelFilter?, ct) SearchResult
        +GetByPidAsync(pid, ct) Programme?
        +GetChannelsAsync(type, ct) IReadOnlyList~ChannelInfo~
    }

    class IStreamService {
        <<interface>>
        +GetStreamsAsync(pid, qualityPrefs, versions, ct) IReadOnlyList~StreamInfo~
        +GetVersionsAsync(pid, ct) IReadOnlyList~StreamVersion~
    }

    class IDownloadService {
        <<interface>>
        +DownloadAsync(pid, progress?, ct) DownloadResult
        +DownloadByIndexAsync(index, progress?, ct) DownloadResult
        +DownloadByUrlAsync(url, progress?, ct) DownloadResult
    }

    class ICacheService {
        <<interface>>
        +GetAllAsync(type, ct) IReadOnlyList~Programme~
        +RefreshAsync(type, ct) void
    }

    class IHistoryService {
        <<interface>>
        +ExistsAsync(pid, ct) bool
        +AddAsync(record, ct) void
        +GetAllAsync(ct) IReadOnlyList~HistoryRecord~
    }

    class IPvrService {
        <<interface>>
        +RunAsync(progress?, ct) IReadOnlyList~DownloadResult~
        +AddAsync(search, ct) void
        +DeleteAsync(name, ct) void
        +GetAllAsync(ct) IReadOnlyList~PvrSearch~
    }

    class IHttpClientService {
        <<interface>>
        +GetStringAsync(url, ct) string
        +GetBytesAsync(url, ct) byte[]
        +DownloadToFileAsync(url, outputPath, progress?, ct) long
    }

    class IHlsDownloader {
        <<interface>>
        +DownloadAsync(stream, outputPath, progress?, ct) long
    }

    class IDashDownloader {
        <<interface>>
        +DownloadAsync(stream, outputPath, progress?, ct) long
    }

    class IPostProcessor {
        <<interface>>
        +RemuxAsync(inputPath, outputPath, subtitlePath?, ct) void
    }

    class IFileSystem {
        <<interface>>
        +FileExists(path) bool
        +CreateDirectory(path) void
        +ReadAllTextAsync(path, ct) string
        +WriteAllTextAsync(path, content, ct) void
    }

    class IExternalProcessRunner {
        <<interface>>
        +FindExecutable(name) string?
        +RunAsync(executable, args, ct) int
    }
```

## DownloadOrchestrator — Internal Workflow

```mermaid
classDiagram
    class DownloadOrchestrator {
        -IProgrammeService _programmeService
        -IStreamService _streamService
        -IHlsDownloader _hlsDownloader
        -IDashDownloader _dashDownloader
        -IPostProcessor _postProcessor
        -ISubtitleService _subtitleService
        -IHistoryService _historyService
        -ICacheService _cacheService
        -IFileSystem _fileSystem
        -DownloadOptions _downloadOptions
        -OutputOptions _outputOptions
        +DownloadAsync(pid, progress?, ct) DownloadResult
        +DownloadByIndexAsync(index, progress?, ct) DownloadResult
        +DownloadByUrlAsync(url, progress?, ct) DownloadResult
    }

    DownloadOrchestrator ..> IProgrammeService : metadata
    DownloadOrchestrator ..> IStreamService : stream discovery
    DownloadOrchestrator ..> IHlsDownloader : HLS download
    DownloadOrchestrator ..> IDashDownloader : DASH download
    DownloadOrchestrator ..> IPostProcessor : remux
    DownloadOrchestrator ..> ISubtitleService : subtitles
    DownloadOrchestrator ..> IHistoryService : history
    DownloadOrchestrator ..> IFileSystem : file ops
```

## BBC Infrastructure — Class Relationships

```mermaid
classDiagram
    class BbcProgrammeService {
        -IHttpClientService _httpClient
        +GetByPidAsync(pid, ct) Programme?
        +SearchAsync(searchTerm, types, channelFilter?, ct) SearchResult
        +GetChannelsAsync(type, ct) IReadOnlyList~ChannelInfo~
    }

    class BbcStreamService {
        -IHttpClientService _httpClient
        +GetStreamsAsync(pid, qualityPrefs, versions, ct) IReadOnlyList~StreamInfo~
        +GetVersionsAsync(pid, ct) IReadOnlyList~StreamVersion~
    }

    class UrlBuilder {
        <<static>>
        +PlaylistJson(pid) string
        +ProgrammePage(pid) string
        +ScheduleJson(channelId, date?) string
        +Search(query, type, page?, pageSize?) string
        +IPlayerEpisode(pid) string
        +SoundsPlay(pid) string
        +EpisodeGuide(pid) string
    }

    class MetadataParser {
        +ParseProgrammeJson(json, type) Programme?
        +ParseProgramme(json, type?)$ Programme?
        +ParseProgrammeElement(element, type)$ Programme?
    }

    class PidValidator {
        <<static>>
        +EnsureValid(pid) void
    }

    class BbcHttpClientService {
        -HttpClient _httpClient
        +GetStringAsync(url, ct) string
        +GetBytesAsync(url, ct) byte[]
        +DownloadToFileAsync(url, outputPath, progress?, ct) long
    }

    BbcProgrammeService --> UrlBuilder : builds URLs
    BbcProgrammeService --> MetadataParser : parses JSON
    BbcProgrammeService --> PidValidator : validates PIDs
    BbcProgrammeService --> BbcHttpClientService : HTTP calls
    BbcStreamService --> UrlBuilder : builds URLs
    BbcStreamService --> PidValidator : validates PIDs
    BbcStreamService --> BbcHttpClientService : HTTP calls
```
