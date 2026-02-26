# get_iplayer → .NET 10 C# Conversion Plan

## Executive Summary

Convert the `get_iplayer` Perl application (~13,800 lines across 2 files) into a modern, maintainable .NET 10 C# solution. The original project is a monolithic Perl script that downloads BBC iPlayer/BBC Sounds content via CLI and an optional CGI web interface. The conversion will decompose this into a well-structured, SOLID-compliant, OWASP-hardened C# solution.

---

## 1. Current Architecture Analysis

### What It Does
- **CLI tool**: Searches, indexes, and downloads BBC iPlayer (TV) and BBC Sounds (radio) programmes
- **PVR**: Scheduled search-and-record functionality
- **Web UI**: CGI-based web interface for managing searches, PVR, and recordings
- **Streaming**: HLS and MPEG-DASH stream downloading with segment-level resume
- **Post-processing**: ffmpeg remuxing (TS→MP4), subtitle conversion (TTML→SRT), metadata tagging (AtomicParsley)

### Current Structure (Perl)
| Package | Lines | Responsibility |
|---------|-------|----------------|
| `main` | ~2500 | CLI, search, download orchestration, HTTP, cache, utilities |
| `Options` | ~530 | Option parsing and persistence (CLI + file) |
| `History` | ~290 | Download history (pipe-delimited flat file) |
| `Programme` | ~1600 | Base programme class |
| `Programme::bbciplayer` | ~2000 | BBC-specific metadata, stream discovery, download |
| `Programme::tv` | ~1040 | TV indexing and channels |
| `Programme::radio` | ~150 | Radio indexing and channels |
| `Streamer::hls` | ~615 | HLS downloader |
| `Streamer::dash` | ~210 | DASH downloader |
| `Pvr` | ~450 | PVR saved searches & scheduling |
| `Tagger` | ~350 | MP4/M4A metadata tagging |
| `get_iplayer.cgi` | ~4010 | Web UI (built-in HTTP server + CGI) |

### Key External Dependencies
- **LWP::UserAgent** → `HttpClient`
- **XML::LibXML** → `System.Xml.Linq` / XPath
- **JSON::PP** → `System.Text.Json`
- **HTML::Entities / HTML::Parser** → `HtmlAgilityPack` or `AngleSharp`
- **IPC::Open3** → `System.Diagnostics.Process`
- **CGI.pm** → ASP.NET Core / Blazor
- **Getopt::Long** → `System.CommandLine`

---

## 2. Target .NET 10 Solution Architecture

### 2.1 Solution Structure

```
GetIPlayer.sln
│
├── src/
│   ├── GetIPlayer.Core/                    # Core domain library
│   │   ├── GetIPlayer.Core.csproj
│   │   ├── Models/
│   │   │   ├── Programme.cs                # Programme entity
│   │   │   ├── ProgrammeType.cs            # TV/Radio enum
│   │   │   ├── StreamInfo.cs               # Stream metadata (mode, bitrate, CDN, URL)
│   │   │   ├── StreamVersion.cs            # Version info (original, signed, etc.)
│   │   │   ├── DownloadResult.cs           # Download outcome
│   │   │   ├── SearchResult.cs             # Search result wrapper
│   │   │   ├── PvrSearch.cs                # PVR saved search definition
│   │   │   ├── HistoryRecord.cs            # Download history entry
│   │   │   ├── ChannelInfo.cs              # Channel metadata
│   │   │   ├── SubtitleData.cs             # Subtitle content
│   │   │   └── TagMetadata.cs              # Metadata for tagging
│   │   ├── Configuration/
│   │   │   ├── AppSettings.cs              # Strongly-typed config POCO
│   │   │   ├── DownloadOptions.cs          # Download-specific options
│   │   │   ├── ProxySettings.cs            # Proxy configuration
│   │   │   ├── OutputOptions.cs            # File naming/output config
│   │   │   └── QualityPreferences.cs       # Quality selection logic
│   │   ├── Interfaces/
│   │   │   ├── IProgrammeService.cs        # Search, index, metadata
│   │   │   ├── IDownloadService.cs         # Download orchestration
│   │   │   ├── IStreamService.cs           # Stream discovery & selection
│   │   │   ├── IHlsDownloader.cs           # HLS protocol handler
│   │   │   ├── IDashDownloader.cs          # DASH protocol handler
│   │   │   ├── ICacheService.cs            # Programme cache management
│   │   │   ├── IHistoryService.cs          # Download history
│   │   │   ├── IPvrService.cs              # PVR operations
│   │   │   ├── IPostProcessor.cs           # ffmpeg/tagging operations
│   │   │   ├── ISubtitleService.cs         # Subtitle download & conversion
│   │   │   ├── IExternalProcessRunner.cs   # Safe process execution
│   │   │   ├── IHttpClientService.cs       # HTTP abstraction
│   │   │   └── IFileSystem.cs              # File system abstraction
│   │   ├── Enums/
│   │   │   ├── StreamProtocol.cs           # HLS, DASH
│   │   │   ├── QualityLevel.cs             # HD, SD, Web, Mobile
│   │   │   └── DownloadStatus.cs           # Success, Failed, Skipped, etc.
│   │   └── Exceptions/
│   │       ├── ProgrammeNotFoundException.cs
│   │       ├── StreamUnavailableException.cs
│   │       ├── GeoBlockedException.cs
│   │       └── DownloadFailedException.cs
│   │
│   ├── GetIPlayer.Infrastructure/          # External concerns implementation
│   │   ├── GetIPlayer.Infrastructure.csproj
│   │   ├── Http/
│   │   │   ├── BbcHttpClientService.cs     # BBC API HTTP client with retry
│   │   │   ├── HttpClientPolicies.cs       # Polly retry/circuit-breaker policies
│   │   │   ├── UserAgentProvider.cs        # User-agent rotation
│   │   │   └── ProxyHandler.cs             # HTTP proxy support
│   │   ├── Bbc/
│   │   │   ├── BbcProgrammeService.cs      # IProgrammeService implementation
│   │   │   ├── BbcStreamService.cs         # IStreamService implementation
│   │   │   ├── ScheduleParser.cs           # Schedule JSON/HTML parsing
│   │   │   ├── MetadataParser.cs           # Programme metadata extraction
│   │   │   ├── PidValidator.cs             # PID format validation
│   │   │   └── UrlBuilder.cs               # BBC URL construction
│   │   ├── Streaming/
│   │   │   ├── HlsDownloader.cs            # HLS segment download with resume
│   │   │   ├── DashDownloader.cs           # DASH segment download with resume
│   │   │   ├── M3u8Parser.cs               # HLS playlist parsing
│   │   │   ├── MpdParser.cs                # DASH MPD manifest parsing
│   │   │   └── SegmentDownloader.cs        # Common segment-level download logic
│   │   ├── PostProcessing/
│   │   │   ├── FfmpegProcessor.cs          # ffmpeg invocation
│   │   │   ├── AtomicParsleyTagger.cs      # Metadata tagging
│   │   │   ├── SubtitleConverter.cs        # TTML → SRT conversion
│   │   │   └── ThumbnailDownloader.cs      # Artwork download
│   │   ├── Persistence/
│   │   │   ├── JsonCacheService.cs         # Programme cache (JSON, not pipe-delimited)
│   │   │   ├── JsonHistoryService.cs       # Download history (JSON)
│   │   │   ├── PvrFileService.cs           # PVR search persistence
│   │   │   ├── OptionsFileService.cs       # Options/preferences file I/O
│   │   │   └── LockFileService.cs          # PVR lock file management
│   │   ├── FileSystem/
│   │   │   ├── SafeFileSystem.cs           # IFileSystem with path validation
│   │   │   └── FileNamingService.cs        # Output filename generation & sanitisation
│   │   └── Process/
│   │       └── SafeProcessRunner.cs        # IExternalProcessRunner (no shell)
│   │
│   ├── GetIPlayer.Application/             # Application/orchestration layer
│   │   ├── GetIPlayer.Application.csproj
│   │   ├── Services/
│   │   │   ├── SearchOrchestrator.cs       # Search flow coordination
│   │   │   ├── DownloadOrchestrator.cs     # Download flow (discover → stream → postprocess)
│   │   │   └── PvrOrchestrator.cs          # PVR run cycle
│   │   ├── Validators/
│   │   │   ├── PidInputValidator.cs        # Validate PID/URL user input
│   │   │   ├── OutputPathValidator.cs      # Validate output directory safety
│   │   │   └── QualityValidator.cs         # Validate quality strings
│   │   └── Mapping/
│   │       └── ProgrammeMapper.cs          # Map API responses → domain models
│   │
│   ├── GetIPlayer.Cli/                     # CLI application
│   │   ├── GetIPlayer.Cli.csproj
│   │   ├── Program.cs                      # Entry point
│   │   ├── Commands/
│   │   │   ├── SearchCommand.cs            # Search/list programmes
│   │   │   ├── GetCommand.cs               # Download by index/PID/URL
│   │   │   ├── PvrCommand.cs               # PVR management subcommands
│   │   │   ├── HistoryCommand.cs           # History listing/management
│   │   │   ├── InfoCommand.cs              # Programme info display
│   │   │   ├── PrefsCommand.cs             # Preferences management
│   │   │   └── RefreshCommand.cs           # Cache refresh
│   │   ├── Output/
│   │   │   ├── ConsoleProgressReporter.cs  # Download progress bar
│   │   │   └── TableFormatter.cs           # Search results table
│   │   └── appsettings.json                # Default configuration
│   │
│   └── GetIPlayer.Web/                     # Web UI (replaces get_iplayer.cgi)
│       ├── GetIPlayer.Web.csproj
│       ├── Program.cs                      # ASP.NET Core entry point
│       ├── Controllers/ (or Minimal APIs)
│       │   ├── SearchController.cs
│       │   ├── DownloadController.cs
│       │   ├── PvrController.cs
│       │   ├── HistoryController.cs
│       │   └── StreamController.cs
│       ├── Pages/ (or Views/)              # Razor Pages/Blazor components
│       ├── wwwroot/                        # Static assets
│       ├── Middleware/
│       │   ├── SecurityHeadersMiddleware.cs
│       │   └── RequestValidationMiddleware.cs
│       └── appsettings.json
│
├── tests/
│   ├── GetIPlayer.Core.Tests/
│   │   └── ...
│   ├── GetIPlayer.Infrastructure.Tests/
│   │   └── ...
│   ├── GetIPlayer.Application.Tests/
│   │   └── ...
│   ├── GetIPlayer.Cli.Tests/
│   │   └── ...
│   └── GetIPlayer.Web.Tests/
│       └── ...
│
├── .editorconfig
├── .gitignore
├── Directory.Build.props                   # Central package versions, TreatWarningsAsErrors
├── Directory.Packages.props                # Central package management
├── global.json                             # Pin .NET 10 SDK
└── README.md
```

### 2.2 NuGet Packages

| Package | Purpose | Replaces |
|---------|---------|----------|
| `System.CommandLine` | CLI parsing, subcommands, help generation | `Getopt::Long` |
| `Microsoft.Extensions.Http` + `Polly` | Typed `HttpClient` with retry/circuit-breaker | `LWP::UserAgent` + retry loop |
| `System.Text.Json` | JSON serialization | `JSON::PP` |
| `System.Xml.Linq` + `System.Xml.XPath` | XML/DASH manifest parsing | `XML::LibXML` |
| `AngleSharp` | HTML parsing | `HTML::Parser` |
| `Microsoft.Extensions.DependencyInjection` | DI container | N/A (improvement) |
| `Microsoft.Extensions.Configuration` | Layered config (JSON, env vars, CLI) | Custom multi-layer Options system |
| `Microsoft.Extensions.Logging` + `Serilog` | Structured logging | `logger()` sub |
| `Microsoft.Extensions.Options` | Strongly-typed options binding | Perl `$opt` hash |
| `Microsoft.AspNetCore.App` | Web UI framework | CGI.pm + raw sockets |
| `FluentValidation` | Input validation | Ad-hoc regex checks |
| `xunit` + `Moq` + `FluentAssertions` | Testing | N/A (no tests exist) |

---

## 3. Phase-by-Phase Implementation Plan

### Phase 1: Project Scaffolding & Core Models (Week 1-2)

**Tasks:**
1. Create solution with all 6 projects + 4 test projects
2. Set up `Directory.Build.props` with:
   - `<TargetFramework>net10.0</TargetFramework>`
   - `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
   - `<Nullable>enable</Nullable>`
   - `<ImplicitUsings>enable</ImplicitUsings>`
   - `<AnalysisLevel>latest-all</AnalysisLevel>`
3. Set up `Directory.Packages.props` for central package management
4. Create `global.json` pinning .NET 10 SDK
5. Define all domain models in `GetIPlayer.Core/Models/`
6. Define all interfaces in `GetIPlayer.Core/Interfaces/`
7. Define enums and custom exceptions
8. Set up `.editorconfig` with C# coding standards

**OWASP consideration:** Establish secure defaults from the start — nullable reference types, immutable models where possible.

---

### Phase 2: Configuration & Options System (Week 2-3)

**Tasks:**
1. Port the 3-layer options cascade (system → user → CLI) to `IConfiguration` with:
   - `appsettings.json` (system defaults)
   - `~/.getiplayer/options.json` (user preferences)
   - Environment variables
   - Command-line arguments (highest priority)
2. Implement `AppSettings`, `DownloadOptions`, `ProxySettings`, `OutputOptions` as strongly-typed POCOs bound via `IOptions<T>` pattern
3. Port `Options::add/del/show/clear` to `OptionsFileService`
4. **Validate all configuration at startup** using `IValidateOptions<T>`
5. Port preset system (named option profiles)

**OWASP consideration (A05: Security Misconfiguration):**
- Validate proxy URLs against an allowlist of schemes (`http`, `https`, `socks5`)
- Reject config values containing path traversal sequences
- Sensitive values (proxy credentials) stored in user secrets, never logged

---

### Phase 3: HTTP Client & BBC API Integration (Week 3-5)

**Tasks:**
1. Implement `BbcHttpClientService` using typed `HttpClient` with `IHttpClientFactory`
2. Port `request_url_retry()` → Polly retry policy (exponential backoff, 3 retries)
3. Port `create_ua()` → `UserAgentProvider` with rotation
4. Port proxy support → `ProxyHandler` using `HttpClientHandler.Proxy`
5. Implement `BbcProgrammeService`:
   - Port schedule fetching (`get_links_schedule_json`)
   - Port metadata parsing (`get_metadata`, `parse_metadata`, `parse_title`)
   - Port PID info fetching (`fetch_pid_info`)
6. Implement `BbcStreamService`:
   - Port version/PID discovery (`get_verpids_json`, `get_verpids_html`)
   - Port stream data resolution (`get_stream_data`, `get_stream_data_cdn`)
   - Port quality/mode selection (`modelist`, `qualities_from_modes`)
7. Implement `ScheduleParser` and `MetadataParser`
8. Implement `PidValidator` (port `REGEX_PID` pattern: `^[b-df-hj-np-tv-z0-9]{8,}$`)
9. Implement `UrlBuilder` for BBC API endpoint construction

**OWASP consideration (A08: Software and Data Integrity Failures):**
- Validate all API response schemas before use
- Pin TLS 1.2+ minimum via `HttpClientHandler.SslProtocols`
- Validate SSL certificates (no `ServerCertificateCustomValidationCallback` bypass)
- Log and reject unexpected redirects to non-BBC domains

---

### Phase 4: Stream Downloading (HLS & DASH) (Week 5-7)

**Tasks:**
1. Implement `M3u8Parser`:
   - Port `parse_m3u_attribs()` and `parse_attributes()`
   - Correctly handle multi-variant and media playlists
2. Implement `MpdParser`:
   - Port DASH MPD XML parsing using `System.Xml.Linq`
   - Port `generate_segments()` for segment URL construction
3. Implement `SegmentDownloader`:
   - Segment-by-segment download with resume support
   - Progress reporting via `IProgress<T>`
   - Cancellation support via `CancellationToken`
4. Implement `HlsDownloader`:
   - Port `Streamer::hls::get()` and `fetch()`
   - Playlist variant selection by quality preference
5. Implement `DashDownloader`:
   - Port `Streamer::dash::get()` and audio+video muxing
6. Implement download resume tracking (replace `.txt` resume files with JSON state files)

**OWASP consideration (A03: Injection):**
- Validate all URLs from playlists are `https://` to BBC CDN domains
- Never interpolate playlist data into shell commands
- Limit segment count and total download size to prevent resource exhaustion

---

### Phase 5: Post-Processing & Tagging (Week 7-8)

**Tasks:**
1. Implement `SafeProcessRunner`:
   - Use `Process.Start()` with `UseShellExecute = false`
   - Pass arguments as array (no shell interpretation)
   - Capture stdout/stderr via `RedirectStandardOutput`/`RedirectStandardError`
   - Implement timeout and cancellation
   - Validate executable paths against resolved absolute paths
2. Implement `FfmpegProcessor`:
   - Port `postproc()` — TS→MP4 remux, audio+video merge, subtitle embedding
   - Port `ffmpeg_init()` — version detection and feature gating
   - Build ffmpeg arguments programmatically (never string concatenation)
3. Implement `AtomicParsleyTagger`:
   - Port `tag_cmd()` and `tag_file_mp4()`
   - Map programme metadata → tag arguments
4. Implement `SubtitleConverter`:
   - Port `ttml_to_srt()` — TTML XML → SRT text conversion
5. Implement `ThumbnailDownloader`

**OWASP consideration (A03: Injection):**
- **Never** use `UseShellExecute = true` or `cmd /c` / `bash -c`
- Validate ffmpeg/AtomicParsley paths via `File.Exists()` on resolved path
- Sanitise all metadata values passed as command arguments
- Set process working directory and restrict `PATH`

---

### Phase 6: Cache, History & PVR (Week 8-9)

**Tasks:**
1. Implement `JsonCacheService`:
   - Replace pipe-delimited `.cache` files with JSON
   - Implement cache expiry and incremental refresh
   - File-based locking for concurrent access
2. Implement `JsonHistoryService`:
   - Replace pipe-delimited `download_history` with JSON
   - Port `add()`, `load()`, `check()`, `get_record()`
3. Implement `PvrFileService`:
   - Port PVR search save/load/delete
   - One JSON file per saved search in PVR directory
4. Implement `PvrOrchestrator`:
   - Port `Pvr::run()` and `run_scheduler()`
   - Lock file management to prevent concurrent PVR runs
5. Implement `LockFileService`:
   - Port stale lock detection (process alive check)
   - Use `FileStream` with `FileShare.None` for atomic lock creation

**OWASP consideration (A01: Broken Access Control):**
- Set file permissions on cache/history/PVR files (`600` on Unix via Mono.Posix or `chmod`)
- Validate PVR search names (alphanumeric + hyphens only, no path traversal)
- Sanitise all filenames derived from programme data

---

### Phase 7: CLI Application (Week 9-10)

**Tasks:**
1. Set up `System.CommandLine` with root command and subcommands:
   ```
   getiplayer search <term> [--type tv|radio] [--channel <regex>] [--long]
   getiplayer get <index|pid|url> [--quality hd,sd] [--subtitles] [--output <dir>]
   getiplayer pvr run|add|del|list|enable|disable [--name <name>]
   getiplayer history [--list] [--clear] [--since <date>]
   getiplayer info <pid>
   getiplayer prefs add|del|show|clear
   getiplayer refresh [--type tv|radio]
   ```
2. Implement each command handler delegating to application services
3. Implement `ConsoleProgressReporter` with download progress bar
4. Implement `TableFormatter` for search result display
5. Wire up DI container in `Program.cs`
6. Support `--webrequest` compatibility mode (URL-encoded options) if needed for web UI

**OWASP consideration (A03: Injection):**
- Use `System.CommandLine` parsing exclusively — never manually parse `args[]`
- Validate `--output` path is within allowed directories
- Validate `--pid` format via `PidValidator` before any API call
- Sanitise `--search` regex (reject regex with catastrophic backtracking potential)

---

### Phase 8: Web UI (ASP.NET Core) (Week 10-13)

**Tasks:**
1. Create ASP.NET Core project with Razor Pages or Blazor Server
2. Port CGI routing to controller/page structure:
   - `SearchController` — programme search and browsing
   - `DownloadController` — trigger and monitor downloads
   - `PvrController` — PVR CRUD and run trigger
   - `HistoryController` — recording history
   - `StreamController` — file streaming to browser
3. Implement real-time download progress via SignalR
4. Port HTML/CSS layout to modern responsive design
5. Implement `SecurityHeadersMiddleware`:
   ```csharp
   // Content-Security-Policy, X-Content-Type-Options, X-Frame-Options,
   // Strict-Transport-Security, Referrer-Policy, Permissions-Policy
   ```
6. Implement `RequestValidationMiddleware` for input sanitisation
7. Add Swagger/OpenAPI documentation for API endpoints

**OWASP considerations (multiple categories):**

| OWASP | Mitigation |
|-------|------------|
| **A01: Broken Access Control** | Add authentication (ASP.NET Core Identity or API key), role-based authorization, enforce HTTPS redirect |
| **A02: Cryptographic Failures** | HTTPS-only (HSTS header), encrypt stored proxy credentials using `DataProtectionProvider` |
| **A03: Injection** | Use parameterised queries, Razor auto-encoding, never `Html.Raw()` with user data |
| **A04: Insecure Design** | Rate limiting on download endpoints, maximum concurrent downloads, bind to `localhost` by default |
| **A05: Security Misconfiguration** | Disable detailed error pages in production, remove server header, enforce minimal CORS |
| **A06: Vulnerable Components** | Dependabot alerts, `dotnet list package --vulnerable` in CI |
| **A07: Auth Failures** | Cookie-based auth with `HttpOnly`, `Secure`, `SameSite=Strict` flags, optional JWT for API |
| **A08: Integrity Failures** | Anti-forgery tokens on all forms (`[ValidateAntiForgeryToken]`), SRI for static assets |
| **A09: Logging Failures** | Structured logging with Serilog, audit log for downloads/PVR changes, never log credentials |
| **A10: SSRF** | Validate all user-supplied URLs against BBC domain allowlist, reject private IP ranges |

---

### Phase 9: Testing (Ongoing, but dedicated sprint Week 13-15)

**Tasks:**
1. **Unit Tests** (xUnit + Moq):
   - `PidValidator` — valid/invalid PID formats
   - `M3u8Parser` — parse multi-variant & media playlists
   - `MpdParser` — parse DASH manifests
   - `SubtitleConverter` — TTML→SRT conversion accuracy
   - `MetadataParser` — title parsing edge cases
   - `QualityValidator` — quality string validation
   - `FileNamingService` — filename sanitisation, substitution patterns
   - `OutputPathValidator` — path traversal rejection
   - `SafeProcessRunner` — argument escaping
2. **Integration Tests**:
   - `BbcHttpClientService` with `MockHttpMessageHandler`
   - `JsonCacheService` — read/write/expire cycle
   - `JsonHistoryService` — add/check/load
   - Web API endpoint tests with `WebApplicationFactory`
3. **Security Tests**:
   - Path traversal fuzzing on output paths
   - XSS payload testing on search terms via web API
   - SSRF testing on proxy settings
   - Regex DoS testing on search patterns
4. **End-to-End Tests**:
   - CLI `search` → `get` → verify downloaded file
   - Web UI search → download → history check

**Target: >80% code coverage for Core + Infrastructure**

---

### Phase 10: CI/CD, Documentation & Polish (Week 15-16)

**Tasks:**
1. GitHub Actions workflow:
   - Build → Test → SAST (`dotnet format`, security analyzers) → Publish
   - `dotnet list package --vulnerable` check
   - Cross-platform matrix (Linux, macOS, Windows)
2. Publish as:
   - Self-contained single-file executables per platform
   - Docker container for web UI
   - `dotnet tool` (global tool)
3. Update `README.md` with new installation and usage instructions
4. Migration guide for existing users (config file format changes)
5. Man page → `--help` comprehensive help system via `System.CommandLine`

---

## 4. OWASP Top 10 Compliance Matrix

| # | OWASP Category | Current Risk | .NET 10 Mitigation |
|---|---------------|-------------|---------------------|
| A01 | **Broken Access Control** | CGI has zero authentication, binds 0.0.0.0, arbitrary output paths | ASP.NET Core Identity auth, localhost default binding, path allowlisting, `[Authorize]` attributes |
| A02 | **Cryptographic Failures** | HTTP-only web server, proxy creds stored in plain text, no HSTS | HTTPS with HSTS enforcement, `DataProtectionProvider` for stored secrets, TLS 1.2+ pinning |
| A03 | **Injection** | Windows `system()` shell execution, two-arg `open()`, CGI params in commands | `Process.Start(UseShellExecute=false)`, `System.CommandLine` parsing, parameterised process args, Razor auto-encoding |
| A04 | **Insecure Design** | No rate limiting, no download caps, monolithic trust model | Rate limiting middleware, max concurrent downloads, defence-in-depth with service boundaries |
| A05 | **Security Misconfiguration** | No security headers, verbose errors exposed to clients, world-readable files | `SecurityHeadersMiddleware` (CSP, HSTS, X-Frame-Options), `IsDevelopment()` error detail gating, restrictive file permissions |
| A06 | **Vulnerable & Outdated Components** | Perl modules with no automated vulnerability scanning | Central package management, `dotnet list package --vulnerable`, Dependabot, `<AnalysisLevel>latest-all</AnalysisLevel>` |
| A07 | **Identification & Auth Failures** | No authentication whatsoever on web UI | ASP.NET Core Identity, `HttpOnly`+`Secure`+`SameSite=Strict` cookies, optional API key auth |
| A08 | **Software & Data Integrity Failures** | No CSRF protection, no SRI, CGI form data not validated | Anti-forgery tokens (`[ValidateAntiForgeryToken]`), SRI hashes on static assets, `FluentValidation` on all inputs |
| A09 | **Security Logging & Monitoring Failures** | Basic `logger()` to stderr, no audit trail | Serilog structured logging, audit events for downloads/config changes, log sanitisation (no PII/credentials) |
| A10 | **Server-Side Request Forgery** | User-controlled URLs passed to HTTP client, `prepend:` proxy mode | URL domain allowlist (BBC domains only), reject RFC 1918/loopback addresses, remove `prepend:` proxy mode |

---

## 5. C# Best Practices Checklist

### Language & Compiler
- [x] `<Nullable>enable</Nullable>` — null safety across the solution
- [x] `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
- [x] `<AnalysisLevel>latest-all</AnalysisLevel>` — all code quality analyzers
- [x] `.editorconfig` with consistent formatting rules
- [x] `record` types for immutable DTOs (e.g., `Programme`, `StreamInfo`)
- [x] `required` keyword on properties that must be set
- [x] `init`-only setters where mutation is not needed
- [x] `file`-scoped namespaces for cleaner code
- [x] Primary constructors where appropriate

### Architecture
- [x] **SOLID principles** — single responsibility per class, dependency inversion via interfaces
- [x] **Clean Architecture** — Core has zero dependencies, Infrastructure implements interfaces
- [x] **Dependency Injection** — all services registered in DI container, no `new` for services
- [x] **Options pattern** — `IOptions<T>` / `IOptionsSnapshot<T>` for configuration
- [x] **Async/await** throughout — all I/O operations are `async Task<T>`
- [x] **CancellationToken** propagated through all async call chains
- [x] **IAsyncDisposable** for resources (HTTP clients, file streams)

### Error Handling
- [x] Custom exception hierarchy (`GetIPlayerException` base)
- [x] `ILogger<T>` structured logging (not string concatenation)
- [x] Global exception handler middleware (web) / handler (CLI)
- [x] Never catch and swallow exceptions silently
- [x] Use `Result<T>` pattern for expected failures (e.g., download failures)

### Performance
- [x] `HttpClient` via `IHttpClientFactory` (connection pooling, DNS refresh)
- [x] `System.Text.Json` source generators for AOT-friendly serialization
- [x] `ReadOnlySpan<char>` / `StringSplitOptions` for parsing instead of regex where possible
- [x] `Channel<T>` for producer/consumer download pipeline
- [x] `ArrayPool<byte>` for segment download buffers

### Security Coding
- [x] Never use string interpolation for process arguments
- [x] Always validate input at system boundaries (CLI args, web requests, API responses)
- [x] Use `Path.GetFullPath()` + path prefix check for file operations
- [x] Sanitise programme data before using in filenames or process arguments
- [x] Use `SecureString` or `DataProtectionProvider` for stored credentials

---

## 6. Data Format Migration

| Data | Perl Format | .NET Format |
|------|------------|-------------|
| Programme cache | Pipe-delimited text (`.cache`) | JSON (`.cache.json`) |
| Download history | Pipe-delimited text (`download_history`) | JSON (`history.json`) |
| User options | Key-value text (`options`) | JSON (`options.json`) |
| PVR searches | Key-value text (`pvr/<name>`) | JSON (`pvr/<name>.json`) |
| Cookie storage | Netscape cookie file | `HttpClient` `CookieContainer` (in-memory) |

A one-time migration utility should be provided to convert existing Perl-format data files to the new JSON format.

---

## 7. Risk Assessment & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| BBC API changes during development | High — could break stream discovery | Abstract API layer behind interfaces; integration tests with recorded responses |
| ffmpeg version compatibility | Medium — different builds have different flags | Feature-detect via `ffmpeg -version` parsing, gate features on version |
| .NET 10 preview stability | Low — .NET 10 GA by Nov 2025 | Pin SDK in `global.json`, use only stable APIs |
| Large Perl codebase complexity | Medium — 14K lines of dense Perl | Phase-by-phase port with tests validating each module |
| Cross-platform file path handling | Medium — Windows vs Unix paths | Use `Path.Combine()`, `Path.DirectorySeparatorChar`, test on all 3 OS |

---

## 8. Estimated Timeline

| Phase | Description | Duration | Cumulative |
|-------|-------------|----------|------------|
| 1 | Scaffolding & Core Models | 2 weeks | Week 2 |
| 2 | Configuration System | 1 week | Week 3 |
| 3 | HTTP & BBC API | 2 weeks | Week 5 |
| 4 | HLS/DASH Downloading | 2 weeks | Week 7 |
| 5 | Post-Processing & Tagging | 1 week | Week 8 |
| 6 | Cache, History & PVR | 1 week | Week 9 |
| 7 | CLI Application | 1 week | Week 10 |
| 8 | Web UI (ASP.NET Core) | 3 weeks | Week 13 |
| 9 | Testing Sprint | 2 weeks | Week 15 |
| 10 | CI/CD & Polish | 1 week | Week 16 |

**Total estimated effort: ~16 weeks (1 developer)**

---

## 9. Key Perl → C# Translation Patterns

| Perl Pattern | C# Equivalent |
|-------------|---------------|
| `package Programme; sub new { bless {}, shift }` | `public class Programme { ... }` (proper OOP) |
| `$prog->{field}` hash access | Strongly-typed properties with validation |
| `$ua->get($url)` | `await _httpClient.GetAsync(url, ct)` |
| `run_cmd(@args)` via `IPC::Open3` | `Process.Start(new ProcessStartInfo { ... })` |
| `open(FH, "< $file")` | `await File.ReadAllTextAsync(path)` or `FileStream` |
| Perl regex `=~ /pattern/` | `Regex.IsMatch()` or `[GeneratedRegex]` source gen |
| `Getopt::Long::GetOptions()` | `System.CommandLine` with typed bindings |
| `CGI->new()` + form handling | ASP.NET Core model binding + `[FromForm]` |
| `$SIG{INT} = sub { cleanup() }` | `Console.CancelKeyPress += (s, e) => Cleanup()` |
| `eval { ... }; if ($@)` | `try { ... } catch (Exception ex)` |
| `use Storable qw(dclone)` | Serialization-based clone or copy constructors |
| `encode_entities($str)` | Razor auto-encoding / `HtmlEncoder.Default.Encode()` |

---

## 10. Getting Started (First Commands)

```bash
# Install .NET 10 SDK
# Create solution
dotnet new sln -n GetIPlayer
dotnet new classlib -n GetIPlayer.Core -o src/GetIPlayer.Core -f net10.0
dotnet new classlib -n GetIPlayer.Infrastructure -o src/GetIPlayer.Infrastructure -f net10.0
dotnet new classlib -n GetIPlayer.Application -o src/GetIPlayer.Application -f net10.0
dotnet new console -n GetIPlayer.Cli -o src/GetIPlayer.Cli -f net10.0
dotnet new webapp -n GetIPlayer.Web -o src/GetIPlayer.Web -f net10.0
dotnet new xunit -n GetIPlayer.Core.Tests -o tests/GetIPlayer.Core.Tests -f net10.0
dotnet new xunit -n GetIPlayer.Infrastructure.Tests -o tests/GetIPlayer.Infrastructure.Tests -f net10.0
dotnet new xunit -n GetIPlayer.Application.Tests -o tests/GetIPlayer.Application.Tests -f net10.0
dotnet new xunit -n GetIPlayer.Cli.Tests -o tests/GetIPlayer.Cli.Tests -f net10.0
dotnet new xunit -n GetIPlayer.Web.Tests -o tests/GetIPlayer.Web.Tests -f net10.0

# Add all projects to solution
dotnet sln add src/**/*.csproj tests/**/*.csproj

# Add project references
dotnet add src/GetIPlayer.Infrastructure reference src/GetIPlayer.Core
dotnet add src/GetIPlayer.Application reference src/GetIPlayer.Core
dotnet add src/GetIPlayer.Cli reference src/GetIPlayer.Application src/GetIPlayer.Infrastructure
dotnet add src/GetIPlayer.Web reference src/GetIPlayer.Application src/GetIPlayer.Infrastructure

# Add test references
dotnet add tests/GetIPlayer.Core.Tests reference src/GetIPlayer.Core
dotnet add tests/GetIPlayer.Infrastructure.Tests reference src/GetIPlayer.Infrastructure src/GetIPlayer.Core
dotnet add tests/GetIPlayer.Application.Tests reference src/GetIPlayer.Application src/GetIPlayer.Core
dotnet add tests/GetIPlayer.Cli.Tests reference src/GetIPlayer.Cli
dotnet add tests/GetIPlayer.Web.Tests reference src/GetIPlayer.Web
```
