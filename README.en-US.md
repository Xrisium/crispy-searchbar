Read this in other languages: [简体中文](README.md), **English**

<br>
<div align="center">
  <img src="assets/icon/crispy-searchbar-256x.png" width="20%">
</div>
<br>

# Crispy Searchbar

A lightweight, modern and fast global search bar for Windows. Built with C# and Avalonia UI, it is designed around fast response, low background resource usage, and clean separation between search modes.

This project has **no** intention of becoming an integrated or all-in-one search tool. It is aimed at users who have a clear search or query intent and do not want to be distracted by irrelevant search results.

## Features

<br>
<div align="center">
  <img src="assets/preview.png" width="80%">
</div>
<br>

- **Quick summon**: stays resident in the system tray; press the hotkey (default `Alt+Space`) to bring up the search bar instantly.
- **Launch at sign-in**: optionally start automatically after signing in to Windows and stay quietly in the system tray.
- **Full-screen friendly**: by default the global shortcut stays inactive while the foreground app is full-screen, so games and videos are not interrupted; clicking the tray icon still opens the search bar.
- **Mode switching**: tap the mode key (default `Tab`) to switch to the next search mode while keeping your input; hold the mode key to open the mode wheel and switch with `↑` / `↓` or the mouse wheel.
- **Customizable modes**: users can enable or disable any search mode at any time from the settings panel, and drag modes to reorder them in the switcher wheel.
- **Adjustable position**: the search bar appears centered on the primary screen by default; you can nudge its position with a pixel offset in the settings panel, restore the default position in one click, and candidates plus the mode wheel flip upwards automatically when the bar sits near the bottom of the screen.
- **Follow System Theme**: Supports light/dark modes and can automatically switch according to the system theme.

## Supported search modes

- **Web search**: search your input with the preconfigured search engine in the default browser. Supports [Baidu](https://www.baidu.com), [Google](https://www.google.com/) and [Bing](https://www.bing.com/).
- **Wikipedia**: look up your input on [Wikipedia](https://en.wikipedia.org/); the Wikipedia language follows the UI language automatically.
- **Ask DeepSeek**: ask your question in the [DeepSeek web app](https://chat.deepseek.com/) using the default browser. Requires a DeepSeek account and signing in beforehand in the browser.
- **Dictionary**: English–Chinese and Chinese–English word lookup with real-time candidates as you type. Lookups run offline using [CC-CEDICT](https://www.mdbg.net/chinese/dictionary?page=cedict) (Chinese → English) and [ECDICT](https://github.com/skywind3000/ECDICT) (English → Chinese). The offline dictionaries ship with the app itself (embedded inside the executable in the single-file build), so no separate download and no extra data files are needed. Both directions accept the same set of custom dictionary files: `.txt` (CC-CEDICT text, or generic lines of "headword + tab + definition"), `.csv`, `.gz`/`.zip` archives, and [StarDict](https://en.wikipedia.org/wiki/StarDict) dictionaries (`.ifo`, with `.idx` and `.dict`/`.dict.dz` of the same base name beside it). The direction comes from which setting you point at — Chinese–English or English–Chinese. The current version does not support additional languages yet.

## Requirements

- Windows 10 x64
- .NET SDK 10

## Portable single-file build

```powershell
pwsh ./scripts/publish-portable.ps1
```

The script builds a **self-contained win-x64 single executable** (about 73 MB) with the `PortableWinX64` publish profile and writes it to `artifacts/crispy-searchbar-{version}-win-x64.exe`, printing its size and SHA-256. The artifact:

- needs no .NET runtime installed — copy it anywhere and double-click.
- contains no files besides the executable itself: both bundled dictionaries (gzip) and the third-party notices/license texts are embedded in the app.
- creates only `settings.json` next to the executable on first run; if that directory is not writable (for example under `Program Files` or on read-only media) the configuration falls back to `%LOCALAPPDATA%\CrispySearchbar\settings.json`, and the settings window shows the path actually in use.
- repairs the launch-at-sign-in registry entry automatically on the next start after the executable is moved.
- still extracts native Skia/HarfBuzz/Angle libraries into `%TEMP%\.net\CrispySearchbar\…` on first run (the system temp directory, not the app directory) — an inherent limitation of single-file Avalonia apps on Windows.

Use the normal `dotnet build` / `dotnet run` flow for development; the two do not interfere.

## Repository layout

```text
src/CrispySearchbar/           Avalonia UI app (window, tray, Windows hotkey)
src/CrispySearchbar.Core/      Platform-independent core (settings, modes, URL building, dictionary index)
tests/CrispySearchbar.Core.Tests/  Unit tests for the core logic
licenses/                      Centralized full texts of third-party licenses
assets/icon/                   App icon source PNG and multi-size ICO assets
locales/                       Per-language UI translations (see locales/README.md)
data/cc-cedict/                Bundled CC-CEDICT data (gzip, embedded into the app as a resource)
data/ecdict/                   Bundled ECDICT data (gzip, embedded into the app as a resource)
scripts/publish-portable.ps1   Portable single-file publish script
```

## License

The project code is licensed under the [MIT License](LICENSE).

Third-party dependency and dictionary data licenses are summarized in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

Full texts are centralized under `licenses/`; the source, retrieval date and attribution for CC-CEDICT and ECDICT are registered in `THIRD_PARTY_NOTICES.md`, and the data directories hold only the data files.
