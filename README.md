# Crispy Searchbar (酥脆搜索)

A tiny, modern global search bar for Windows. Built with C# and Avalonia UI, designed to be keyboard-first, fast, and low-overhead in the background.

## Status

Current milestone: search shell with a functional dictionary mode.

- The visible UI is a single rounded capsule search bar. Dictionary candidates and details appear in an on-demand popup below it.
- `Alt+Space` shows/hides the search bar; `Esc` hides it; the app keeps running in the tray with “show/hide”, “open config file” and “exit” menu items.
- Tab cycles through modes: web search, ask AI, dictionary.
- Web search and “ask AI” open the default browser using a configurable URL template (the query is URL-encoded into `{0}`).
- Dictionary mode performs offline 英汉 / 汉英 lookup against CC-CEDICT:
  - real-time candidate suggestions as you type;
  - English queries are case-insensitive and ignore trailing punctuation;
  - Chinese queries accept both simplified and traditional forms;
  - ↑/↓ moves the selection, Enter opens the definition detail, mouse click also selects;
  - typing is non-blocking and stale query results are discarded.
- After Enter executes a browser search, the search bar hides automatically; reopen it with `Alt+Space` or the tray menu.

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

## Dictionary data

The dictionary mode needs CC-CEDICT’s `cedict_ts.u8` text file.

- The repository includes a bundled copy under `data/cc-cedict/`, which is copied next to the executable at build time.
- To use your own data, put a file named `cedict_ts.u8` in `%LOCALAPPDATA%\CrispySearchbar\`, or point `dictionaryFilePath` at any file path in `settings.json`. The user-located file takes precedence and is never overwritten by the bundled copy.
- Latest data can be downloaded from https://www.mdbg.net/chinese/dictionary?page=cedict (CC BY-SA 4.0; see `data/cc-cedict/NOTICE` and `data/cc-cedict/LICENSE.txt`).

## Configuration

On first run the app creates `settings.json` in the same directory as the executable (in development this is the `bin` output directory; in a published build it sits next to the exe), so users can edit it directly. A GUI settings page can reuse the same file/model later.

Example file:

```json
{
  "theme": "system",
  "searchEngine": "baidu",
  "clearQueryOnHide": true,
  "askAiUrlTemplate": "https://chat.deepseek.com/?q={0}",
  "dictionaryFilePath": null
}
```

- `theme`: `system`, `light` or `dark`.
- `searchEngine`: `baidu` (default), `google` or `bing`.
- `clearQueryOnHide`: `true` (default) clears the typed query whenever the search bar is hidden; `false` keeps it.
- `askAiUrlTemplate`: must contain `{0}`, replaced by the URL-encoded query.
- `dictionaryFilePath`: optional absolute path to a CC-CEDICT file; `null` uses the user data directory, then the bundled file.

If the file is missing or corrupt, defaults are used.

## Repository layout

```text
src/CrispySearchbar/           Avalonia UI application (window, tray, Windows hotkey)
src/CrispySearchbar.Core/      Platform-independent core (settings, modes, URL building, dictionary index)
tests/CrispySearchbar.Core.Tests/  Unit tests for core logic
data/cc-cedict/                Bundled CC-CEDICT data with its license/notice
```

## License

Project code is MIT. Third-party dependencies and dictionary data have their own licenses; see [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) and `data/cc-cedict/NOTICE`.
