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
- **Mode switching**: tap the mode key (default `Tab`) to switch to the next search mode while keeping your input; hold the mode key to open the mode wheel and switch with `↑` / `↓` or the mouse wheel.
- **Customizable modes**: users can enable or disable any search mode at any time from the settings panel, and drag modes to reorder them in the switcher wheel.
- **Follow System Theme**: Supports light/dark modes and can automatically switch according to the system theme.

## Supported search modes

- **Web search**: search your input with the preconfigured search engine in the default browser. Supports [Baidu](https://www.baidu.com), [Google](https://www.google.com/) and [Bing](https://www.bing.com/).
- **Wikipedia**: look up your input on [Wikipedia](https://en.wikipedia.org/); the Wikipedia language follows the UI language automatically.
- **Ask DeepSeek**: ask your question in the [DeepSeek web app](https://chat.deepseek.com/) using the default browser. Requires a DeepSeek account and signing in beforehand in the browser.
- **Dictionary**: English–Chinese and Chinese–English word lookup with real-time candidates as you type. Lookups run offline using [CC-CEDICT](https://www.mdbg.net/chinese/dictionary?page=cedict) (Chinese → English) and [ECDICT](https://github.com/skywind3000/ECDICT) (English → Chinese). The offline dictionaries are installed together with Crispy Searchbar, so no separate download is needed; you can also download dictionary files that match the required format and configure them yourself. The current version does not support additional languages yet.

## Requirements

- Windows 10 x64
- .NET SDK 10

## Repository layout

```text
src/CrispySearchbar/           Avalonia UI app (window, tray, Windows hotkey)
src/CrispySearchbar.Core/      Platform-independent core (settings, modes, URL building, dictionary index)
tests/CrispySearchbar.Core.Tests/  Unit tests for the core logic
licenses/                      Centralized full texts of third-party licenses
assets/icon/                   App icon source PNG and multi-size ICO assets
locales/                       Per-language UI translations (see locales/README.md)
data/cc-cedict/                Bundled CC-CEDICT data with its own license/notice
data/ecdict/                   Bundled ECDICT data with its own license/notice
```

## License

The project code is licensed under the [MIT License](LICENSE).

Third-party dependency and dictionary data licenses are summarized in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

Full texts are centralized under `licenses/`; CC-CEDICT and ECDICT additionally keep `LICENSE.txt`/`NOTICE` in their data directories.