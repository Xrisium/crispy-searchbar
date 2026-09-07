# Crispy Searchbar (酥脆搜索)

A tiny, modern global search bar for Windows. Built with C# and Avalonia UI, designed to be keyboard-first, fast, and low-overhead in the background.

## Status

Current milestone: resident search shell.

- The visible UI is a single rounded capsule search bar; candidate panels are only added later for modes that need them (e.g. dictionary).
- `Alt+Space` shows/hides the search bar; `Esc` hides it; the app keeps running in the tray with “show/hide”, “open config file” and “exit” menu items.
- Tab cycles through modes: web search, ask AI, dictionary (dictionary is still a placeholder).
- Web search and “ask AI” open the default browser using a configurable URL template (the query is URL-encoded into `{0}`).
- After Enter executes a search, the search bar hides automatically; reopen it with `Alt+Space` or the tray menu.
- The default web search engine is Baidu; Google and Bing can be selected in the config file.

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

On first run the app creates `settings.json` in the same directory as the executable (in development this is the `bin` output directory; in a published build it sits next to the exe), so users can edit it directly. A GUI settings page can reuse the same file/model later.

Example file:

```json
{
  "theme": "system",
  "searchEngine": "baidu",
  "clearQueryOnHide": true,
  "askAiUrlTemplate": "https://chat.deepseek.com/?q={0}"
}
```

- `theme`: `system`, `light` or `dark`.
- `searchEngine`: `baidu` (default), `google` or `bing`.
- `clearQueryOnHide`: `true` (default) clears the typed query whenever the search bar is hidden; `false` keeps it.
- `askAiUrlTemplate`: must contain `{0}`, replaced by the URL-encoded query.

If the file is missing or corrupt, defaults are used.

## Repository layout

```text
src/CrispySearchbar/           Avalonia UI application (window, tray, Windows hotkey)
src/CrispySearchbar.Core/      Platform-independent core (settings, modes, URL building)
tests/CrispySearchbar.Core.Tests/  Unit tests for core logic
```

## License

Project code is MIT. Third-party dependencies and their licenses are listed in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).


