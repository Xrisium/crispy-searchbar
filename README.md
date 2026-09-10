Read this in other languages: **简体中文**, [English](README.en-US.md)

<br>
<div align="center">
  <img src="assets/icon/crispy-searchbar-256x.png" width="20%">
</div>
<br>

# 酥脆搜索（Crispy Searchbar）

一款轻量、现代、快速的 Windows 全局搜索框。使用 C# 与 Avalonia UI 构建，以响应迅速、后台低占用、搜索模式分离为设计目标。

此项目**没有**制作任何整合搜索或全量搜索模式的打算，而是致力于为有确定搜索或查询意图、不愿意被无关搜索结果干扰的用户打造。

## 功能特色

<br>
<div align="center">
  <img src="assets/preview.png" width="80%">
</div>
<br>

- **快速呼出**：常驻于系统托盘，按下快捷键（默认为 Alt + 空格键）即可呼出搜索框。
- **模式切换**：短按模式切换键（默认为 Tab）快速切换到下一搜索模式（保留输入内容）；长按模式切换键呼出模式选择轮盘，并可以用 ↑、↓ 键或鼠标滚轮进行切换。
- **模式自定义**：用户可随时在配置设置面板中启用或禁用任意搜索模式，并可对各个模式在切换轮盘中的位置进行拖拽排序。
- **跟随系统主题**：支持浅色/深色模式，并可根据系统主题自动跟随切换。

## 支持搜索模式

- **网页搜索**：使用预先配置的搜索引擎在默认浏览器中搜索输入的内容，支持[百度](https://www.baidu.com)、[谷歌](https://www.google.com/)和[必应](https://www.bing.com/)。
- **维基百科**：在 [维基百科](https://zh.wikipedia.org/) 中查询输入的内容，所使用的维基百科语种将随界面语言自动切换。
- **询问 Deepseek**：使用默认浏览器，在 [DeepSeek 网页版](https://chat.deepseek.com/) 中询问输入的问题。需要用户拥有 DeepSeek 账户并提前在浏览器中登录。
- **词典**：支持英汉和汉英词汇查询，随输入实时显示候选词。使用 [CC-CEDICT](https://www.mdbg.net/chinese/dictionary?page=cedict)（汉 → 英）与 [ECDICT](https://github.com/skywind3000/ECDICT)（英 → 汉）进行离线查询。离线词典将会随 Crispy Searchbar 本体一同安装，无需另外下载。两个方向支持同一组自备词典格式：`.txt`（CC-CEDICT 文本，或每行“词头 + 制表符 + 释义”的通用文本）、`.csv`、`.gz`/`.zip` 压缩文件，以及 [StarDict](https://en.wikipedia.org/wiki/StarDict) 词库（`.ifo`，需同目录配套 `.idx` 与 `.dict`/`.dict.dz`）；方向由你把它填在“汉英”还是“英汉”设置项决定。当前版本暂不支持更多语种。

## 环境要求

- Windows 10 x64
- .NET SDK 10

## 仓库结构

```text
src/CrispySearchbar/           Avalonia UI 应用（窗口、托盘、Windows 热键）
src/CrispySearchbar.Core/      与平台无关的核心（设置、模式、URL 构建、词典索引）
tests/CrispySearchbar.Core.Tests/  核心逻辑单元测试
licenses/                     集中存放的第三方许可证全文
assets/icon/                  应用图标源 PNG 与多尺寸 ICO 素材
locales/                      各语言界面翻译（见 locales/README.md）
data/cc-cedict/               内置 CC-CEDICT 数据及其许可证/声明
data/ecdict/                  内置 ECDICT 数据及其许可证/声明
```

## 许可证

项目代码采用 [MIT 许可证](LICENSE)。

第三方依赖与词典数据的许可证汇总见 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)；

全文集中存放于 `licenses/`，CC-CEDICT 与 ECDICT 还在各自数据目录中保留了 `LICENSE.txt`/`NOTICE`。
