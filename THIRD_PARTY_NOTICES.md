# Third-Party Notices

本项目的代码使用 MIT License（见 `LICENSE`）。下面的第三方依赖和组件各自拥有独立许可证；使用或再分发本项目（含编译产物）时，须遵守这些许可证的条款。

记录生成时间：2026-09-07。许可证信息读取自 NuGet 包元数据；升级依赖后应重新核对并更新本文件。

## 运行时依赖（应用发布时会涉及）

| 包 | 版本 | 许可证 |
|---|---|---|
| Avalonia | 12.1.2 | MIT |
| Avalonia.Angle.Windows.Natives | 2.1.27548.20260419 | BSD-3-Clause（ANGLE Project） |
| Avalonia.BuildServices | 11.3.2 | MIT |
| Avalonia.Desktop | 12.1.2 | MIT |
| Avalonia.FreeDesktop | 12.1.2 | MIT |
| Avalonia.FreeDesktop.AtSpi | 12.1.2 | MIT |
| Avalonia.HarfBuzz | 12.1.2 | MIT |
| Avalonia.Native | 12.1.2 | MIT |
| Avalonia.Remote.Protocol | 12.1.2 | MIT |
| Avalonia.Skia | 12.1.2 | MIT |
| Avalonia.Themes.Fluent | 12.1.2 | MIT |
| Avalonia.Win32 | 12.1.2 | MIT |
| Avalonia.X11 | 12.1.2 | MIT |
| HarfBuzzSharp | 8.3.1.3 | MIT |
| HarfBuzzSharp.NativeAssets.Linux | 8.3.1.3 | MIT |
| HarfBuzzSharp.NativeAssets.macOS | 8.3.1.3 | MIT |
| HarfBuzzSharp.NativeAssets.WebAssembly | 8.3.1.3 | MIT |
| HarfBuzzSharp.NativeAssets.Win32 | 8.3.1.3 | MIT |
| MicroCom.Runtime | 0.11.6 | MIT |
| SkiaSharp | 3.119.4 | MIT |
| SkiaSharp.NativeAssets.Linux | 3.119.4 | MIT |
| SkiaSharp.NativeAssets.macOS | 3.119.4 | MIT |
| SkiaSharp.NativeAssets.WebAssembly | 3.119.4 | MIT |
| SkiaSharp.NativeAssets.Win32 | 3.119.4 | MIT |
| Tmds.DBus.Protocol | 0.94.1 | MIT |

## 测试与开发依赖（不进发布产物）

| 包 | 版本 | 许可证 |
|---|---|---|
| Microsoft.CodeCoverage | 18.9.0 | MIT |
| Microsoft.NET.Test.Sdk | 18.9.0 | MIT |
| Microsoft.TestPlatform.ObjectModel | 18.9.0 | MIT |
| Microsoft.TestPlatform.TestHost | 18.9.0 | MIT |
| xunit | 2.9.3 | Apache-2.0 |
| xunit.abstractions | 2.0.3 | Apache-2.0 |
| xunit.analyzers | 1.18.0 | Apache-2.0 |
| xunit.assert | 2.9.3 | Apache-2.0 |
| xunit.core | 2.9.3 | Apache-2.0 |
| xunit.extensibility.core | 2.9.3 | Apache-2.0 |
| xunit.extensibility.execution | 2.9.3 | Apache-2.0 |
| xunit.runner.visualstudio | 4.0.0 | Apache-2.0 |

## 说明

- 许可证文本以各包自身携带的许可证文件或官方项目仓库为准；各上游许可证全文可在对应 NuGet 包缓存或上游仓库中找到。
- 正式发布二进制时，发布流程应把涉及的许可证全文（含本文件）随产物一并提供。
- 后续引入任何新的第三方包、字体、图标或词典数据时，先确认其许可证与再分发条款，再同步更新本文件；后续引入词典等数据资产时，应在其文件旁单独提供 `LICENSE`/`NOTICE` 记录来源与许可证。

