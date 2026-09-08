阅读英文版：[English](README.md)

# Crispy Searchbar（酥脆搜索）

一款轻量、现代、快速的 Windows 全局搜索框。使用 C# 与 Avalonia UI 构建，以键盘优先、响应迅速、后台低占用为设计目标。

## 当前状态

当前里程碑：可用的搜索壳与功能完整的词典模式。

- 常驻界面是单个圆角胶囊搜索框；词典候选与释义详情按需出现在下方的浮层中。
- `Alt+Space` 显示/隐藏搜索框；`Esc` 或点击其它窗口会隐藏它；应用常驻系统托盘，菜单包含“显示 / 隐藏搜索框”“设置”和“退出”。
- 快速按 Tab 按“设置 → 模式”中配置的顺序循环切换模式（默认：网页搜索 → 维基百科 → 问问大肥鱼 → 词典）；长按 Tab 会打开纵向模式选择器，松开 Tab 即完成切换（鼠标滚轮或 ↑/↓ 可移动高亮）。
- 网页搜索、维基百科与问问大肥鱼使用网址模板在默认浏览器中打开结果（查询词会被 URL 编码并替换到 `{0}`）。维基百科模式的站点跟随当前翻译文件中的 `wikipediaLanguage` 元数据（例如 `zh-Hans` 使用 zh.wikipedia.org、`en` 使用 en.wikipedia.org）。
- 词典模式使用 CC-CEDICT（汉 → 英）与 ECDICT（英 → 汉）进行离线查询：
  - 输入时实时提供候选词；
  - 英文查询不区分大小写，并忽略结尾标点；
  - 中文查询同时支持简体和繁体；
  - ↑/↓ 移动选择，Enter 打开释义详情，鼠标单击直接打开词条；
  - 输入过程不阻塞界面，过期查询结果会被丢弃。
- 词典模式下按 Enter 打开详情，不隐藏搜索框；网页类模式执行搜索后会自动隐藏搜索框，可用 `Alt+Space` 或托盘菜单再次唤出。

## 环境要求

- Windows 10 x64
- .NET SDK 10

## 运行

```powershell
dotnet run --project src/CrispySearchbar
```

## 构建与测试

```powershell
dotnet build
dotnet test
```

## 词典数据

词典模式需要两套内置数据：CC-CEDICT 的 `cedict_ts.u8`（汉 → 英）与 ECDICT 的 `ecdict.csv`（英 → 汉）。

- 仓库分别在 `data/cc-cedict/` 与 `data/ecdict/` 下内置了这两套数据，构建时会复制到可执行文件旁边。
- 如需使用自己的数据，可将 `cedict_ts.u8` 和/或 `ecdict.csv` 放到 `%LOCALAPPDATA%\CrispySearchbar\`，或在 `settings.json` 中用 `dictionaryFilePath` / `ecdictFilePath` 指向任意文件路径。用户提供的文件优先，且不会被内置副本覆盖。
- 最新数据可从 https://www.mdbg.net/chinese/dictionary?page=cedict 下载（CC BY-SA 4.0；见 `data/cc-cedict/NOTICE` 与 `data/cc-cedict/LICENSE.txt`），以及从 https://github.com/skywind3000/ECDICT 下载（MIT；见 `data/ecdict/NOTICE` 与 `data/ecdict/LICENSE.txt`）。

## 配置

首次运行时，应用会在可执行文件所在目录创建 `settings.json`（开发环境中即 `bin` 输出目录；发布构建中位于 exe 旁边），用户可以直接编辑它。

通过托盘菜单 →“设置”可打开同一份 `settings.json` 的可视化设置编辑器。编辑器不会保存第二份设置：打开时读取文件，“保存”时写回文件，运行中的应用会立即应用已保存的值（主题、语言、搜索设置、启用的模式与顺序、词典路径）。窗口底部显示的配置文件路径可点击“打开配置文件”，用系统默认的 JSON 应用打开 `settings.json`。手动编辑文件仍然完全受支持；该文件始终是设置的唯一来源。

设置窗口由 `AppSettings` 属性注解（`[Setting]`）自动生成。新增一个可配置属性并补充对应的本地化文案后，编辑界面会自动出现新行，无需编写逐字段的窗口代码；“模式”分类就是这样一个数据驱动行，可逐个启用/禁用模式，并用拖拽或上/下按钮调整顺序。

底部的“关于”分区仅作信息展示：显示应用版本，提供 GitHub 仓库与 `THIRD_PARTY_NOTICES.md` 的链接，并在确认后提供把 `settings.json` 恢复为默认值的重置按钮。

示例文件：

```json
{
  "language": "system",
  "theme": "system",
  "searchEngine": "baidu",
  "clearQueryOnHide": true,
  "askAiUrlTemplate": "https://chat.deepseek.com/?q={0}",
  "modePreferences": [
    { "key": "web-search", "enabled": true },
    { "key": "wikipedia", "enabled": true },
    { "key": "ask-ai", "enabled": true },
    { "key": "dictionary", "enabled": true }
  ],
  "dictionaryFilePath": null,
  "ecdictFilePath": null
}
```

- `language`：`system`（默认，跟随 Windows 界面语言）或 `locales/` 中内置翻译对应的任意 BCP 47 代码（当前为 `zh-Hans` 与 `en`）。未知代码或系统语言无匹配时回退英文。
- `theme`：`system`、`light` 或 `dark`。
- `searchEngine`：`baidu`（默认）、`google` 或 `bing`。
- `clearQueryOnHide`：`true`（默认）在搜索框隐藏时清空已输入内容；`false` 则保留。
- `askAiUrlTemplate`：必须包含 `{0}`，该占位符会被 URL 编码后的查询词替换。
- `modePreferences`：内置模式的有序列表；`enabled: false` 会将该模式从 Tab 切换与模式选择器中移除，但仍保留其列表位置。至少需要启用一个模式；全禁用列表会在加载时归一化为默认值。
- `dictionaryFilePath`：可选的 CC-CEDICT（汉 → 英）UTF-8 文本文件绝对路径，通常为 `cedict_ts.u8`（`.u8`）；`null` 时依次使用用户数据目录与内置文件。
- `ecdictFilePath`：可选的 ECDICT（英 → 汉）CSV 文件绝对路径，通常为 `ecdict.csv`（`.csv`）；`null` 时依次使用用户数据目录与内置文件。

界面翻译位于 `locales/*.json`（见 `locales/README.md`）。`en.json` 是完整回退基准：翻译文件可只提供部分词条，缺失或空值会回退英文。新增语言时复制 `en.json` 为 `{code}.json`、翻译词条并填写 `nativeName` 与 `wikipediaLanguage`，重新构建即可；应用自动发现该文件，无需修改任何代码。

如果配置文件缺失或损坏，则使用默认值。非法模式列表（未知键、重复键、缺失模式或全部禁用）会在加载时被归一化，并将修正后的文件写回。

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

项目代码采用 MIT 许可证。第三方依赖与词典数据的许可证汇总见 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)；全文集中存放于 `licenses/`，CC-CEDICT 与 ECDICT 还在各自数据目录中保留了 `LICENSE.txt`/`NOTICE`。
