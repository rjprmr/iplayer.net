# Technology Choices & Design Decisions

## Technology Stack

| Layer | Technology | Version | Purpose |
|-------|-----------|---------|---------|
| **Runtime** | .NET | 10 | Target framework for all projects |
| **Web UI** | ASP.NET Core Razor Pages | — | Server-rendered HTML pages with form-based interaction |
| **Real-time** | SignalR | — | WebSocket-based download progress broadcasting |
| **CLI** | System.CommandLine | — | Modern CLI argument parsing with strongly-typed commands |
| **HTTP** | HttpClient (IHttpClientFactory) | — | Named `"BbcClient"` with proxy, timeout, and UA rotation |
| **Logging** | Serilog | — | Structured logging with console sink |
| **DI** | Microsoft.Extensions.DependencyInjection | — | Standard .NET DI container, shared registration via `AddGetIPlayerServices()` |
| **Configuration** | IOptions&lt;T&gt; pattern | — | `DownloadOptions`, `OutputOptions`, `ProxySettings` |
| **Serialisation** | System.Text.Json | — | JSON parsing for BBC API responses and file persistence |
| **Streaming** | Custom HLS/DASH | — | M3U8 and MPD parsers with concurrent segment downloaders |
| **Post-processing** | ffmpeg | External | Media remuxing and subtitle embedding |
| **Persistence** | JSON files | — | Cache, history, PVR searches, options — all stored as JSON on disk |

---

## Architectural Patterns

### Clean Architecture / Ports & Adapters

The solution follows a layered architecture with clear dependency inversion:

```
┌─────────────────────────────────────┐
│   Hosts (Web, CLI)                  │  ← Entry points, DI composition roots
├─────────────────────────────────────┤
│   Application (Orchestrators)       │  ← Use-case coordinators
├─────────────────────────────────────┤
│   Core (Models, Interfaces, Enums)  │  ← Domain contracts (no dependencies)
├─────────────────────────────────────┤
│   Infrastructure (Implementations)  │  ← Concrete adapters for BBC, file I/O, etc.
└─────────────────────────────────────┘
```

- **Core** has zero project dependencies — it defines the "ports" (interfaces) and domain types
- **Infrastructure** implements the ports — it is the "adapter" layer
- **Application** orchestrates workflows using only interfaces from Core
- **Hosts** compose the DI container and wire everything together

### Background Processing (Web)

The web host uses a producer-consumer pattern for downloads:

1. **Producer**: Razor Pages write `DownloadRequest` into a `Channel<DownloadRequest>` (unbounded)
2. **Consumer**: `DownloadBackgroundService` (a `BackgroundService`) reads from the channel
3. **Progress**: Real-time updates are pushed to the browser via `DownloadHub` (SignalR)
4. **Tracking**: `DownloadTracker` maintains in-memory state in a `ConcurrentDictionary`

### Singleton-Scoped Services

Most infrastructure services are registered as **singletons** because they are stateless or manage their own thread safety (e.g., `JsonHistoryService` uses `SemaphoreSlim`). The `DownloadBackgroundService` creates a DI scope per download to resolve scoped services if needed.

---

## Security Considerations (OWASP)

The codebase includes several explicit OWASP-aligned safeguards:

| OWASP | Mitigation | Implementation |
|-------|-----------|----------------|
| **A03 — Injection** | External process arguments are never built from raw user input | `SafeProcessRunner` sanitises arguments; `PidValidator` enforces alphanumeric PID format |
| **A05 — Security Misconfiguration** | TLS 1.2+ enforced for all BBC API calls | `BbcHttpClientService` validates SSL and uses `HttpClient` defaults |
| **A08 — Software & Data Integrity** | Only known BBC domains are allowed for HTTP requests | `BbcHttpClientService.ValidateUrl()` checks against an allowlist |
| **A10 — Server-Side Request Forgery** | API URLs are constructed from templates, not user input | `UrlBuilder` uses hardcoded `https://www.bbc.co.uk` base with `Uri.EscapeDataString` |
| **Input Validation** | Search terms truncated, PID format enforced, batch sizes capped | `SearchModel.MaxSearchLength`, `PidValidator.EnsureValid()`, `MaxBatchSize = 50` |
| **Path Traversal** | File paths are validated against a safe base directory | `SafeFileSystem` prevents path traversal attacks |

---

## Persistence Strategy

All persistence uses **JSON files** on the local file system under `~/.get_iplayer/`:

| Data | File(s) | Service |
|------|---------|---------|
| Application settings | `options.json` | `OptionsFileService` |
| Programme cache | `cache_tv.json`, `cache_radio.json` | `JsonCacheService` (4-hour TTL) |
| Download history | `history.json` | `JsonHistoryService` (with `SemaphoreSlim` file locking) |
| PVR searches | `pvr/<name>.json` (one file per search) | `PvrFileService` |
| Instance locking | Lock files | `LockFileService` |

This mirrors the original Perl `get_iplayer` behaviour and avoids requiring any database setup.

---

## Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| **Razor Pages over Blazor/MVC** | Simple form-based UI, server-rendered. No need for SPA complexity. Each page maps to a clear use case. |
| **SignalR for progress** | Enables real-time download progress without polling. Natural fit for ASP.NET Core. |
| **Channel&lt;T&gt; for download queue** | High-performance, thread-safe producer-consumer without external message broker. |
| **Dual host (Web + CLI)** | Web for casual use, CLI for scripting and automation. Both share the same Application/Infrastructure layers. |
| **JSON file persistence** | Compatibility with original `get_iplayer` profile structure; zero database dependencies. |
| **Custom HLS/DASH parsers** | BBC uses standard HLS/DASH; custom parsers allow fine-grained control over quality selection and segment download concurrency. |
| **Source-generated logging** | `LoggerMessage` attribute (via `partial` methods) used throughout for high-performance structured logging. |
| **Serilog** | Consistent structured logging across both hosts with minimal configuration. |

---

## Test Projects

| Project | Covers |
|---------|--------|
| `GetIPlayer.Core.Tests` | Domain model validation, enum behaviour |
| `GetIPlayer.Application.Tests` | Orchestrator logic, search pipeline integration |
| `GetIPlayer.Infrastructure.Tests` | BBC service tests (URL building, metadata parsing, programme service), persistence |
| `GetIPlayer.Web.Tests` | Razor Page model tests (SearchModel, IndexModel) |
| `GetIPlayer.Cli.Tests` | CLI command tests |
