# Third-Party Notices

本项目的代码使用 MIT License（见根目录 `LICENSE`）。下面的第三方依赖、组件与数据资产各自拥有独立许可证；使用或再分发本项目（含编译产物）时，须遵守这些许可证的条款。

记录生成时间：2026-09-09；最近更新：2026-09-11（词典数据许可证改为集中管理）。许可证信息读取自 NuGet 包元数据与数据发布页；升级依赖或更新数据后应重新核对并更新本文件。

## 许可证原文集中管理

第三方许可证的完整原文统一放在根目录 `licenses/`，按 SPDX 许可证标识命名：

| SPDX 许可证 | 完整原文 |
|---|---|
| MIT | [licenses/MIT.txt](licenses/MIT.txt) |
| BSD-3-Clause | [licenses/BSD-3-Clause.txt](licenses/BSD-3-Clause.txt) |
| Apache-2.0 | [licenses/Apache-2.0.txt](licenses/Apache-2.0.txt) |
| CC BY-SA 4.0 | [licenses/CC-BY-SA-4.0.txt](licenses/CC-BY-SA-4.0.txt) |
| ISC | [licenses/ISC.txt](licenses/ISC.txt) |

数据资产（词典、字体等）不再在数据目录旁保留 `LICENSE`/`NOTICE`：许可证全文统一放在 `licenses/`，各数据源的来源、获取日期与署名声明统一登记在本文件“捆绑数据资产”一节。单独再分发某个数据文件时，须一并提供本文件与 `licenses/` 下的许可证原文。

## 内置 UI 图标资源（随应用分发）

| 资源 | 来源 | 许可证 |
|---|---|---|
| `src/CrispySearchbar/Assets/Icons/lucide-globe.svg`、`lucide-book-open-text.svg`、`lucide-sliders-horizontal.svg`、`lucide-palette.svg`、`lucide-search.svg`、`lucide-settings.svg`、`lucide-info.svg`、`lucide-keyboard.svg` | Lucide（lucide-icons/lucide）`main` commit `a537cb6eb323b885f4c60baf3cec1a995982d167` 的 `icons/globe.svg`、`icons/book-open-text.svg`、`icons/sliders-horizontal.svg`、`icons/palette.svg`、`icons/search.svg`、`icons/settings.svg`、`icons/info.svg`、`icons/keyboard.svg` | [ISC](licenses/ISC.txt)，Copyright (c) 2026 Lucide Icons and Contributors |
| `src/CrispySearchbar/Assets/Icons/deepseek-whale.svg` | DeepSeek（deepseek-ai/deepseek-harness）`master` commit `c389f96bf3a9b6807cb71ed6bdad5849be0df6d8` 的 `apps/web/public/favicon.svg` | [MIT](licenses/MIT.txt)，Copyright (c) 2026 DeepSeek |
| `src/CrispySearchbar/Assets/Icons/wikipedia-w.svg` | Wikimedia Commons：[File:Wikipedia's W.svg](https://commons.wikimedia.org/wiki/File:Wikipedia%27s_W.svg)（官方 Wikipedia favicon，W 字形源自 Hoefler Text，Jonathan Hoefler 设计，2007-06-26） | 公有领域（[PD-text](https://en.wikipedia.org/wiki/Wikipedia:Public_domain)：单一字母字形不具著作权门槛）；使用须遵循 [Wikimedia 视觉识别指引](https://wikimediafoundation.org/brand-stewardship/brand-use/)。W 是 Wikimedia Foundation 的 Wikipedia 标识组成部分，本项目用于标示维基百科搜索模式，非 Wikimedia 官方背书 |
## 运行时依赖（应用发布时会涉及）

| 包 | 版本 | 许可证 |
|---|---|---|
| Avalonia | 12.1.2 | [MIT](licenses/MIT.txt) |
| Avalonia.Angle.Windows.Natives | 2.1.27548.20260419 | [BSD-3-Clause](licenses/BSD-3-Clause.txt) |
| Avalonia.BuildServices | 11.3.2 | [MIT](licenses/MIT.txt) |
| Avalonia.Desktop | 12.1.2 | [MIT](licenses/MIT.txt) |
| Avalonia.FreeDesktop | 12.1.2 | [MIT](licenses/MIT.txt) |
| Avalonia.FreeDesktop.AtSpi | 12.1.2 | [MIT](licenses/MIT.txt) |
| Avalonia.HarfBuzz | 12.1.2 | [MIT](licenses/MIT.txt) |
| Avalonia.Native | 12.1.2 | [MIT](licenses/MIT.txt) |
| Avalonia.Remote.Protocol | 12.1.2 | [MIT](licenses/MIT.txt) |
| Avalonia.Skia | 12.1.2 | [MIT](licenses/MIT.txt) |
| Avalonia.Themes.Fluent | 12.1.2 | [MIT](licenses/MIT.txt) |
| Avalonia.Win32 | 12.1.2 | [MIT](licenses/MIT.txt) |
| Avalonia.X11 | 12.1.2 | [MIT](licenses/MIT.txt) |
| HarfBuzzSharp | 8.3.1.3 | [MIT](licenses/MIT.txt) |
| HarfBuzzSharp.NativeAssets.Linux | 8.3.1.3 | [MIT](licenses/MIT.txt) |
| HarfBuzzSharp.NativeAssets.macOS | 8.3.1.3 | [MIT](licenses/MIT.txt) |
| HarfBuzzSharp.NativeAssets.WebAssembly | 8.3.1.3 | [MIT](licenses/MIT.txt) |
| HarfBuzzSharp.NativeAssets.Win32 | 8.3.1.3 | [MIT](licenses/MIT.txt) |
| MicroCom.Runtime | 0.11.6 | [MIT](licenses/MIT.txt) |
| SkiaSharp | 3.119.4 | [MIT](licenses/MIT.txt) |
| SkiaSharp.NativeAssets.Linux | 3.119.4 | [MIT](licenses/MIT.txt) |
| SkiaSharp.NativeAssets.macOS | 3.119.4 | [MIT](licenses/MIT.txt) |
| SkiaSharp.NativeAssets.WebAssembly | 3.119.4 | [MIT](licenses/MIT.txt) |
| SkiaSharp.NativeAssets.Win32 | 3.119.4 | [MIT](licenses/MIT.txt) |
| Tmds.DBus.Protocol | 0.94.1 | [MIT](licenses/MIT.txt) |

## 捆绑数据资产

数据目录只保留数据文件本身，不再旁挂 `LICENSE`/`NOTICE`；下表登记各数据源，随后给出各自的来源与署名声明。

| 数据 | 文件 | 版本/获取 | 许可证 |
|---|---|---|---|
| CC-CEDICT | `data/cc-cedict/cedict_1_0_ts_utf-8_mdbg.txt` | 2026-09-10 release，125049 条，获取于 2026-09-11 | [CC BY-SA 4.0](licenses/CC-BY-SA-4.0.txt) |
| ECDICT | `data/ecdict/ecdict.csv` | 2026-09-08 upstream master，约 77 万条 | [MIT](licenses/MIT.txt) |

### CC-CEDICT

- 来源：MDBG（https://www.mdbg.net/chinese/dictionary?page=cedict）；官方发布文件为 `cedict_1_0_ts_utf-8_mdbg.txt.gz`（2026-09-10 release，125049 条）。
- 捆绑的 `cedict_1_0_ts_utf-8_mdbg.txt` 为该官方文件用 gzip 解压后的原样文本，未增加、删除或修改任何词条；MDBG 只提供 UTF-8 纯文本，没有官方 CSV 或 StarDict 发布。
- 许可：Creative Commons Attribution-ShareAlike 4.0 International（[licenses/CC-BY-SA-4.0.txt](licenses/CC-BY-SA-4.0.txt)）。
- 署名：CC-CEDICT 是 Paul Denisowski 发起的 CEDICT 项目的延续，由 MDBG 维护。
- Share-Alike：你对该数据所做的任何改进或增补，都必须以相同的许可证共享。项目代码为 MIT 许可，本数据文件保留其独立的 CC BY-SA 4.0 许可。

### ECDICT

- 来源：https://github.com/skywind3000/ECDICT ，下载地址 https://raw.githubusercontent.com/skywind3000/ECDICT/master/ecdict.csv ，获取于 2026-09-08。
- 捆绑的 `ecdict.csv` 为上游仓库原样文件，未做修改；文件大小 65,933,428 字节，约 77 万条。
- 许可：MIT License，Copyright (c) 2025 Linwei（[licenses/MIT.txt](licenses/MIT.txt)）。

## 测试与开发依赖（不进发布产物）

| 包 | 版本 | 许可证 |
|---|---|---|
| Microsoft.CodeCoverage | 18.9.0 | [MIT](licenses/MIT.txt) |
| Microsoft.NET.Test.Sdk | 18.9.0 | [MIT](licenses/MIT.txt) |
| Microsoft.TestPlatform.ObjectModel | 18.9.0 | [MIT](licenses/MIT.txt) |
| Microsoft.TestPlatform.TestHost | 18.9.0 | [MIT](licenses/MIT.txt) |
| xunit | 2.9.3 | [Apache-2.0](licenses/Apache-2.0.txt) |
| xunit.abstractions | 2.0.3 | [Apache-2.0](licenses/Apache-2.0.txt) |
| xunit.analyzers | 1.18.0 | [Apache-2.0](licenses/Apache-2.0.txt) |
| xunit.assert | 2.9.3 | [Apache-2.0](licenses/Apache-2.0.txt) |
| xunit.core | 2.9.3 | [Apache-2.0](licenses/Apache-2.0.txt) |
| xunit.extensibility.core | 2.9.3 | [Apache-2.0](licenses/Apache-2.0.txt) |
| xunit.extensibility.execution | 2.9.3 | [Apache-2.0](licenses/Apache-2.0.txt) |
| xunit.runner.visualstudio | 4.0.0 | [Apache-2.0](licenses/Apache-2.0.txt) |

## 说明

- 许可证文本以各包自身携带的许可证文件、SPDX 官方文本或官方项目仓库为准；本目录中的 `licenses/` 文件与 NuGet 包缓存/上游仓库中的原文应保持一致。
- 正式发布二进制时，发布流程应把根目录 `THIRD_PARTY_NOTICES.md` 与 `licenses/` 下的许可证全文随产物一并提供。
- 后续引入任何新的第三方包、字体、图标或词典数据时，先确认其许可证与再分发条款，再按本文件“许可证原文集中管理”一节的约定补充原文并更新表格。

