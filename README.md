Read this in [简体中文](README.zh-CN.md)

# Crispy Searchbar

A tiny, modern global search bar for Windows. Built with C# and Avalonia UI, designed to be keyboard-first, fast, and low-overhead in the background.

## Status

Current milestone: search shell with a functional dictionary mode.

- The visible UI is a single rounded capsule search bar. Dictionary candidates and details appear in an on-demand popup below it.
- `Alt+Space` shows/hides the search bar; `Esc` or clicking another window hides it; the app keeps running in the tray with “Show / Hide Search Bar”, “Settings” and “Exit” menu items.
- Quick Tab cycles through modes: Web Search → Wikipedia → Ask DeepSeek → Dictionary; holding Tab opens the vertical mode picker and releasing Tab switches (mouse wheel or ↑/↓ moves the highlight).
- Web Search, Wikipedia and Ask DeepSeek open the default browser using a URL template (the query is URL-encoded into `{0}`). The Wikipedia mode targets zh.wikipedia.org for the Simplified Chinese UI and en.wikipedia.org otherwise, following the interface language.
- Dictionary mode performs offline lookup against CC-CEDICT (Chinese → English) and ECDICT (English → Chinese):
  - real-time candidate suggestions as you type;
  - English queries are case-insensitive and ignore trailing punctuation;
  - Chinese queries accept both simplified and traditional forms;
  - ↑/↓ moves the selection, Enter opens the definition detail, mouse click opens the entry;
  - typing is non-blocking and stale query results are discarded.
- Enter opens dictionary details without hiding the search bar. After Enter executes a browser search instead, the search bar hides automatically; reopen it with `Alt+Space` or the tray menu.

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

The dictionary mode needs two bundled datasets: CC-CEDICT’s `cedict_ts.u8` for Chinese → English, and ECDICT’s `ecdict.csv` for English → Chinese.

- The repository bundles both under `data/cc-cedict/` and `data/ecdict/`; they are copied next to the executable at build time.
- To use your own data, put `cedict_ts.u8` and/or `ecdict.csv` in `%LOCALAPPDATA%\CrispySearchbar\`, or point `dictionaryFilePath` / `ecdictFilePath` at any file path in `settings.json`. User-located files take precedence and are never overwritten by the bundled copies.
- Latest data can be downloaded from https://www.mdbg.net/chinese/dictionary?page=cedict (CC BY-SA 4.0; see `data/cc-cedict/NOTICE` and `data/cc-cedict/LICENSE.txt`) and https://github.com/skywind3000/ECDICT (MIT; see `data/ecdict/NOTICE` and `data/ecdict/LICENSE.txt`).

## Configuration

On first run the app creates `settings.json` in the same directory as the executable (in development this is the `bin` output directory; in a published build it sits next to the exe), so users can edit it directly.

Tray menu → “Settings” opens a visual settings editor for the same `settings.json`. The editor never keeps a second settings store: it reads the file on open, writes the file on “Save”, and the running app immediately re-applies the saved values (theme, language, search settings, dictionary paths). The path shown in the window footer opens `settings.json` in its default JSON application when you click “Open configuration file”. Manually editing the file is still fully supported; the file remains the source of truth.

The settings window is generated from `AppSettings` property annotations (`[Setting]`). Adding a new configurable property plus its localized copy makes a new editing row appear automatically; no per-field window code is needed.

The About section at the bottom is informational: it shows the app version, links to the GitHub repository and `THIRD_PARTY_NOTICES.md`, and offers a reset button that restores `settings.json` to defaults after confirmation.

Example file:

```json
{
  "language": "zh-Hans",
  "theme": "system",
  "searchEngine": "baidu",
  "clearQueryOnHide": true,
  "askAiUrlTemplate": "https://chat.deepseek.com/?q={0}",
  "dictionaryFilePath": null,
  "ecdictFilePath": null
}
```

- `language`: `zh-Hans` (default, Simplified Chinese) or `en` (English). Unsupported values fall back to `zh-Hans`.
- `theme`: `system`, `light` or `dark`.
- `searchEngine`: `baidu` (default), `google` or `bing`.
- `clearQueryOnHide`: `true` (default) clears the typed query whenever the search bar is hidden; `false` keeps it.
- `askAiUrlTemplate`: must contain `{0}`, replaced by the URL-encoded query.
- `dictionaryFilePath`: optional absolute path to a CC-CEDICT (Chinese → English) UTF-8 text file, typically `cedict_ts.u8` (`.u8`); `null` uses the user data directory, then the bundled file.
- `ecdictFilePath`: optional absolute path to an ECDICT (English → Chinese) CSV file, typically `ecdict.csv` (`.csv`); `null` uses the user data directory, then the bundled file.

All user-visible UI text is centralized in `src/CrispySearchbar.Core/Localization/`. Each supported language has a complete `AppStrings` instance registered in `AppLanguage.Supported`; adding another language means adding that instance, registering its code in `AppLanguage.Supported`, and mapping it in `AppStrings.For`.

If the file is missing or corrupt, defaults are used.

## Repository layout

```text
src/CrispySearchbar/           Avalonia UI application (window, tray, Windows hotkey)
src/CrispySearchbar.Core/      Platform-independent core (settings, modes, URL building, dictionary index)
tests/CrispySearchbar.Core.Tests/  Unit tests for core logic
licenses/                     Centralized full texts of third-party licenses
assets/icon/                  App icon master PNG and multi-size ICO source assets
data/cc-cedict/                Bundled CC-CEDICT data with its own license/notice
data/ecdict/                   Bundled ECDICT data with its own license/notice
```

## License

Project code is MIT. Third-party dependency and dictionary data licenses are summarized in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md); full texts live centrally under `licenses/`, and CC-CEDICT and ECDICT additionally keep `LICENSE.txt`/`NOTICE` next to their data.
