# GetIPlayer.NET — Architecture Documentation

This directory contains [C4 model](https://c4model.com/) architecture diagrams and documentation for the **GetIPlayer.NET** solution — a .NET 10 port of the `get_iplayer` BBC programme downloader.

## Diagrams

| Level | File | Description |
|-------|------|-------------|
| **C4-L1** | [C1-Context.md](C1-Context.md) | System Context — actors and external systems |
| **C4-L2** | [C2-Container.md](C2-Container.md) | Container — deployable units and their responsibilities |
| **C4-L3** | [C3-Component.md](C3-Component.md) | Component — key classes within each container |
| **C4-L4** | [C4-Code.md](C4-Code.md) | Code — class-level details for critical workflows |

## Additional Documentation

| File | Description |
|------|-------------|
| [Data-Flow.md](Data-Flow.md) | End-to-end data flow for Search, Download, and PVR workflows |
| [Technology-Choices.md](Technology-Choices.md) | Technology stack, patterns, and design decisions |

## How to View

All diagrams use [Mermaid](https://mermaid.js.org/) syntax. They render natively on:

- **GitHub** — Markdown preview renders Mermaid blocks automatically
- **VS Code** — Install the *Markdown Preview Mermaid Support* extension
- **JetBrains Rider** — Built-in Mermaid support in Markdown preview
- **Online** — Paste into [mermaid.live](https://mermaid.live)
