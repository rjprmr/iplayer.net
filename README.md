## get_iplayer: BBC iPlayer/BBC Sounds Indexing Tool and PVR

A .NET 10 C# rewrite of the original Perl-based [get_iplayer](https://github.com/get-iplayer/get_iplayer) tool.

## Features

* Downloads TV and radio programmes from BBC iPlayer/BBC Sounds
* Allows multiple programmes to be downloaded using a single command
* Indexing of most available iPlayer/Sounds catch-up programmes from previous 30 days (not Red Button, iPlayer Exclusive, or Podcast-only)
* Caching of programme index with automatic updating
* Regex search on programme name
* Regex search on programme description and episode title
* Filter search results by channel
* Direct download via programme ID or URL
* PVR capability for automated recording of saved searches
* HLS and DASH streaming protocol support
* Automatic subtitle download and TTML-to-SRT conversion
* Post-processing with ffmpeg (remux, audio/video merge, metadata tagging)
* HTTP/HTTPS/SOCKS5 proxy support
* Cross-platform: runs on Linux, macOS, and Windows

### Requirements

* [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (10.0 or later)
* [ffmpeg](https://ffmpeg.org/) for media conversion and tagging

**NOTE:**

- **get_iplayer can only search for programmes that were scheduled for broadcast on BBC linear services within the previous 30 days, even if some are available for more than 30 days on the iPlayer/Sounds sites. Red button programmes, iPlayer box sets, web-only content, and BBC podcasts are not searchable. Programmes that are still available after 30 days must be located on the iPlayer/Sounds sites and downloaded directly via PID or URL.**
- **get_iplayer does not support downloading news/sport videos, other embedded media, archive programmes, special collections, educational material, programme clips or any content other than whole episodes of programmes scheduled for broadcast on BBC linear services within the previous 30 days. However, it is generally possible to download other content such as red button programmes, iPlayer box sets, or podcasts directly via PID or URL. get_iplayer DOES NOT support live recording from BBC channels.**

## Project Structure

```
GetIPlayer.slnx                     # .NET 10 solution (slnx format)
src/
  GetIPlayer.Core/                   # Domain models, enums, interfaces, configuration, exceptions
  GetIPlayer.Infrastructure/         # BBC API services, HTTP, streaming, persistence, post-processing
  GetIPlayer.Application/            # Orchestrators (download, search, PVR workflows)
  GetIPlayer.Cli/                    # System.CommandLine CLI application
  GetIPlayer.Web/                    # ASP.NET Core Razor Pages web interface
tests/
  GetIPlayer.Core.Tests/
  GetIPlayer.Infrastructure.Tests/
  GetIPlayer.Application.Tests/
  GetIPlayer.Cli.Tests/
  GetIPlayer.Web.Tests/
```

## Building

```sh
dotnet build GetIPlayer.slnx
```

## Running

```sh
dotnet run --project src/GetIPlayer.Cli
```

Or after publishing:

```sh
get-iplayer --help
```

## Usage

```
get-iplayer <command> [options]

Commands:
  search   Search for BBC programmes
  get      Download programmes by PID, index, or URL
  pvr      Manage PVR (automated recording) saved searches
  history  View and manage download history
  info     Show detailed programme information
  refresh  Refresh the programme cache
  prefs    View and set application preferences

Global options:
  --verbose, -v       Enable verbose/debug logging
  --profile-dir       Override profile directory
```

## Examples

* Search for TV programmes (default type):

    ```sh
    get-iplayer search "doctor who"
    ```

    Search output appears in this format:

    ```
    208:  Doctor Who: Series 7 Part 2 - 1. The Bells of Saint John
           [TV] BBC One | b01rryzz | 00:44:00
    209:  Doctor Who: Series 7 Part 2 - 2. The Rings Of Akhaten
           [TV] BBC One | b01rx0lj | 00:44:00
    ```

* Search for radio programmes:

    ```sh
    get-iplayer search "book at bedtime" --type radio
    ```

* Search for both TV and radio programmes:

    ```sh
    get-iplayer search "doctor who" --type tv --type radio
    ```

* Filter by channel:

    ```sh
    get-iplayer search ".*" --channel "BBC One"
    ```

* Download a programme by index number (from search results):

    ```sh
    get-iplayer get 208
    ```

* Download a programme by PID:

    ```sh
    get-iplayer get b01sc0wf
    ```

* Download a programme by iPlayer URL:

    ```sh
    get-iplayer get https://www.bbc.co.uk/iplayer/episode/b01sc0wf
    ```

* Download a programme from BBC Sounds by URL:

    ```sh
    get-iplayer get https://www.bbc.co.uk/sounds/play/b07gcv34
    ```

* Download multiple programmes:

    ```sh
    get-iplayer get 208 209 210
    ```

* Download with subtitles:

    ```sh
    get-iplayer get b01sc0wf --subtitles
    ```

* Force re-download (ignore history):

    ```sh
    get-iplayer get b01sc0wf --force
    ```

* Show detailed programme info:

    ```sh
    get-iplayer info b01sc0wf
    ```

* Refresh the programme cache:

    ```sh
    get-iplayer refresh
    get-iplayer refresh --type tv
    ```

### PVR (Automated Recording)

* Add a PVR search:

    ```sh
    get-iplayer pvr add "my-search" "doctor who" --type tv
    ```

* List saved PVR searches:

    ```sh
    get-iplayer pvr list
    ```

* Run all PVR searches and download matches:

    ```sh
    get-iplayer pvr run
    ```

* Delete a PVR search:

    ```sh
    get-iplayer pvr delete "my-search"
    ```

### Preferences

* Show current preferences:

    ```sh
    get-iplayer prefs show
    ```

* Set a preference:

    ```sh
    get-iplayer prefs set output-dir /path/to/downloads
    get-iplayer prefs set subtitles true
    get-iplayer prefs set proxy http://proxy:8080
    ```

### Download History

* View download history:

    ```sh
    get-iplayer history list
    ```

* Check if a programme has been downloaded:

    ```sh
    get-iplayer history check b01sc0wf
    ```

* Clear download history:

    ```sh
    get-iplayer history clear
    ```

## Configuration

Preferences are stored in `~/.get_iplayer/options.json`. Available settings:

| Key | Description | Default |
|-----|-------------|---------|
| `output-dir` | Download output directory | Current directory |
| `file-prefix` | Filename prefix template | `""` |
| `subtitles` | Download subtitles | `false` |
| `thumbnail` | Download thumbnail | `false` |
| `tag` | Tag files with metadata | `true` |
| `force` | Force re-download | `false` |
| `overwrite` | Overwrite existing files | `false` |
| `proxy` | HTTP proxy URL | `""` |
| `ffmpeg` | Path to ffmpeg binary | `""` (auto-detect) |

## Running Tests

```sh
dotnet test GetIPlayer.slnx
```

## Architecture

The solution follows Clean Architecture principles:

* **Core** — Domain models, enums, interfaces, and exceptions. No external dependencies.
* **Infrastructure** — Implementations of core interfaces: BBC API clients, HTTP services, HLS/DASH streaming, ffmpeg post-processing, JSON-based persistence.
* **Application** — Orchestrators that coordinate infrastructure services into high-level workflows (download, search, PVR).
* **CLI** — Thin command-line interface using [System.CommandLine](https://github.com/dotnet/command-line-api) that delegates to Application orchestrators.
* **Web** — ASP.NET Core Razor Pages web interface.

Key technical choices:

* Targets **.NET 10** (`net10.0`)
* Central package management (`Directory.Packages.props`)
* `TreatWarningsAsErrors`, `AnalysisLevel=latest-recommended`, nullable reference types
* `LoggerMessage` source generators for high-performance structured logging
* `GeneratedRegex` for compiled regular expressions
* Dependency injection via `Microsoft.Extensions.DependencyInjection`
* Resilient HTTP with `Polly` retry policies

## License

See [LICENSE.txt](LICENSE.txt).

NOTE: Sometimes you may not be able to download a listed programme immediately after broadcast (usually available within 24hrs of airing). Some BBC programmes may not be available from iPlayer/Sounds.
