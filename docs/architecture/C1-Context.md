# C1 — System Context Diagram

The System Context diagram shows GetIPlayer.NET as a single system, the people who use it, and the external systems it interacts with.

```mermaid
C4Context
    title System Context — GetIPlayer.NET

    Person(user, "User", "A person who wants to search for and download BBC iPlayer / BBC Sounds programmes.")

    System(getiplayer, "GetIPlayer.NET", "A .NET 10 application that searches, downloads, and manages BBC programmes via a web UI or CLI.")

    System_Ext(bbcApi, "BBC iPlayer / Sounds APIs", "Provides programme metadata, search results, schedule listings, and media stream manifests (HLS/DASH).")
    System_Ext(ffmpeg, "ffmpeg", "External CLI tool used for remuxing downloaded media segments into MP4/M4A containers.")
    System_Ext(filesystem, "Local File System", "Stores downloaded media files, programme cache, download history, PVR searches, and application settings.")

    Rel(user, getiplayer, "Searches, browses, and downloads programmes via")
    Rel(getiplayer, bbcApi, "Fetches metadata, search results, and media streams from", "HTTPS")
    Rel(getiplayer, ffmpeg, "Invokes for post-processing (remux, subtitle merge)", "Process exec")
    Rel(getiplayer, filesystem, "Reads/writes cache, history, settings, and media files", "File I/O")
```

## Key External Dependencies

| System | Purpose | Protocol |
|--------|---------|----------|
| **BBC iPlayer / Sounds APIs** | Programme search (`ibl` JSON API), metadata (programmes pages), stream discovery (playlist JSON), schedule listings | HTTPS to `www.bbc.co.uk` |
| **ffmpeg** | Remuxes raw HLS/DASH segment files into final MP4/M4A, optionally merges subtitles | Local process execution |
| **Local File System** | Persists JSON cache, download history, PVR saved searches, options, and the final media output | File I/O |

## Actors

| Actor | Description |
|-------|-------------|
| **User** | Interacts through the Razor Pages web UI (browser) or the CLI (`System.CommandLine`-based) |
