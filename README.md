# Crispy Searchbar (酥脆搜索)

A tiny, modern global search bar for Windows. Built with C# and Avalonia UI, designed to be keyboard-first, fast, and low-overhead in the background.

## Status

Current milestone: runnable scaffold. The app launches a search-window shell with three switchable modes (web search, ask AI, dictionary), a local JSON config file, and the project foundation split into platform-independent core and Avalonia app layers.

- Web search and “ask AI” both open the default browser using a configurable URL template (query is URL-encoded into `{0}`).
- Dictionary mode is a placeholder for now.
- `Esc` quits in this development build; global hotkey, tray, and hide-instead-of-quit behavior come next.

## Requirements

- Windows 10 x64
- .NET SDK 10

## Run

```powershell
dotnet run --project src/CrispySearchbar
```

## Build & test

```powershell
dotnet build
dotnet test
```

## Configuration

On first run the app creates `settings.json` under `%APPDATA%\CrispySearchbar` with defaults (theme, search engine URL template, AI chat URL template). Delete the file to reset defaults.

## Repository layout

```text
src/CrispySearchbar/           Avalonia UI application (Windows integration later)
src/CrispySearchbar.Core/      Platform-independent core (settings, modes, URL building)
tests/CrispySearchbar.Core.Tests/  Unit tests for core logic
```

## License

Project code is MIT. Third-party dependencies and their licenses are listed in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
