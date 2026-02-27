# Data Flow Diagrams

End-to-end data flows for the three primary use cases.

---

## 1. Search Flow

The user searches for a BBC programme by keyword. Results are fetched from the BBC iPlayer search API in real time.

```mermaid
sequenceDiagram
    actor User
    participant UI as Web UI / CLI
    participant SO as SearchOrchestrator
    participant BPS as BbcProgrammeService
    participant UB as UrlBuilder
    participant MP as MetadataParser
    participant HTTP as BbcHttpClientService
    participant BBC as BBC iPlayer API

    User->>UI: Enter search term (e.g. "Doctor Who")
    UI->>SO: SearchAsync("Doctor Who", [Tv, Radio])

    loop For each ProgrammeType
        SO->>BPS: SearchAsync("Doctor Who", [type])
        BPS->>UB: Search("Doctor Who", type)
        UB-->>BPS: https://ibl.api.bbc.co.uk/ibl/v1/search?q=Doctor+Who&mediatype=...
        BPS->>HTTP: GetStringAsync(url)
        HTTP->>BBC: GET /ibl/v1/search?q=...
        BBC-->>HTTP: JSON response
        HTTP-->>BPS: JSON string
        BPS->>MP: ParseSearchResults(json)
        MP-->>BPS: SearchResult (list of Programme)
        BPS-->>SO: SearchResult
    end

    SO-->>UI: Aggregated SearchResult
    UI-->>User: Display programme list
```

### Key Details

- The `SearchOrchestrator` iterates over requested `ProgrammeType[]` (TV, Radio) and calls `BbcProgrammeService.SearchAsync` for each
- `UrlBuilder.Search()` constructs the BBC ibl JSON API URL with safe URI escaping
- `MetadataParser` converts raw JSON into `Programme` domain records
- Optional `channelFilter` is applied as a post-filter on the aggregated results
- Input is validated: search term is truncated at 200 characters in the web UI

---

## 2. Download Flow

The user initiates a download for a programme by PID. The system discovers streams, downloads segments, post-processes, and records history.

```mermaid
sequenceDiagram
    actor User
    participant UI as Web UI / CLI
    participant Queue as Channel~DownloadRequest~
    participant BG as DownloadBackgroundService
    participant DO as DownloadOrchestrator
    participant HS as IHistoryService
    participant BPS as BbcProgrammeService
    participant BSS as BbcStreamService
    participant HLS as HlsDownloader
    participant DASH as DashDownloader
    participant SEG as SegmentDownloader
    participant HTTP as BbcHttpClientService
    participant BBC as BBC APIs
    participant PP as FfmpegProcessor
    participant FFmpeg as ffmpeg (process)
    participant FS as File System
    participant Hub as DownloadHub (SignalR)

    User->>UI: Select programme (PID: b0ABC123)

    alt Web UI
        UI->>Queue: Write DownloadRequest
        Queue->>BG: Read DownloadRequest
        BG->>DO: DownloadAsync("b0ABC123", progress)
    else CLI
        UI->>DO: DownloadAsync("b0ABC123", progress)
    end

    DO->>HS: ExistsAsync("b0ABC123")
    HS-->>DO: false (not in history)

    DO->>BPS: GetByPidAsync("b0ABC123")
    BPS->>HTTP: GET programme page
    HTTP->>BBC: HTTPS request
    BBC-->>HTTP: JSON metadata
    HTTP-->>BPS: JSON string
    BPS-->>DO: Programme

    DO->>BSS: GetStreamsAsync("b0ABC123", qualityPrefs, versions)
    BSS->>HTTP: GET playlist.json
    HTTP->>BBC: HTTPS request
    BBC-->>HTTP: JSON with stream URLs
    HTTP-->>BSS: JSON string
    BSS-->>DO: List~StreamInfo~ (ordered by quality)

    alt HLS stream selected
        DO->>HLS: DownloadAsync(streamInfo, outputPath, progress)
        HLS->>HTTP: GET M3U8 master playlist
        HTTP->>BBC: HTTPS
        BBC-->>HTTP: M3U8 content
        HLS->>SEG: Download all TS segments
        SEG->>HTTP: GET each segment (concurrent)
        HTTP->>BBC: HTTPS (segment data)
        SEG-->>HLS: Raw segment file
        HLS-->>DO: bytesDownloaded
    else DASH stream selected
        DO->>DASH: DownloadAsync(streamInfo, outputPath, progress)
        DASH->>HTTP: GET MPD manifest
        DASH->>SEG: Download DASH segments
        SEG-->>DASH: Raw segment file
        DASH-->>DO: bytesDownloaded
    end

    DO->>PP: RemuxAsync(rawFile, finalFile.mp4)
    PP->>FFmpeg: ffmpeg -y -i raw -c copy final.mp4
    FFmpeg-->>PP: Exit code 0
    PP-->>DO: Success

    DO->>HS: AddAsync(HistoryRecord)

    DO-->>BG: DownloadResult (Success)
    BG->>Hub: SendAsync("DownloadProgress", completed)
    Hub-->>User: Real-time progress via WebSocket
```

### Key Details

- **Web path**: Download requests go through a `Channel<DownloadRequest>` → `DownloadBackgroundService` → `DownloadOrchestrator`
- **CLI path**: The CLI calls `DownloadOrchestrator` directly
- **History check**: Downloads are skipped if the PID is already in history (unless `--force` is used)
- **Stream selection**: Streams are sorted by quality preference, then by bitrate (descending)
- **Segment download**: `SegmentDownloader` handles concurrent segment downloads with configurable parallelism
- **Post-processing**: `FfmpegProcessor` remuxes raw segments into MP4/M4A via `SafeProcessRunner` (no shell injection)
- **Progress reporting**: The web UI receives real-time updates through SignalR (`DownloadHub`)

---

## 3. PVR (Automated Recording) Flow

The PVR system runs saved searches against the programme cache and automatically downloads new matches.

```mermaid
sequenceDiagram
    actor User
    participant UI as Web UI / CLI
    participant PO as PvrOrchestrator
    participant CS as ICacheService
    participant PFS as PvrFileService
    participant DO as DownloadOrchestrator
    participant FS as File System

    User->>UI: Trigger PVR run (or scheduled)
    UI->>PO: RunAllAsync(progress)

    PO->>CS: RefreshAsync(Tv)
    CS->>FS: Check cache age / fetch from BBC
    PO->>CS: RefreshAsync(Radio)

    PO->>PFS: RunAsync(progress)
    PFS->>FS: Read PVR search JSON files
    FS-->>PFS: List~PvrSearch~

    loop For each enabled PvrSearch
        PFS->>CS: GetAllAsync(type)
        CS-->>PFS: List~Programme~ (cached)
        PFS->>PFS: Filter programmes matching search term
        loop For each matching programme
            PFS->>DO: DownloadAsync(pid, progress)
            DO-->>PFS: DownloadResult
        end
    end

    PFS-->>PO: List~DownloadResult~
    PO-->>UI: Summary (total, successful)
    UI-->>User: PVR results
```

### Key Details

- Cache is always refreshed before a PVR run to ensure the latest listings are available
- Each `PvrSearch` is stored as a separate JSON file in the PVR directory
- Only enabled searches (`IsEnabled == true`) are executed
- Matching uses the search term against the cached programme list
- Downloads go through the same `DownloadOrchestrator` pipeline as manual downloads
