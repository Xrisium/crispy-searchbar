using CrispySearchbar.Core.Localization;

namespace CrispySearchbar.Core.Configuration;

/// <summary>应用设置。新增字段必须提供可直接运行的默认值。</summary>
public sealed class AppSettings
{
    /// <summary>
    /// 界面语言：system（默认，跟随系统）或 locales 中提供的 BCP 47 代码（如 zh-Hans、en）。
    /// 未知值由 TranslationCatalog 回退英文。
    /// </summary>
    [Setting(SettingsSection.General, SettingEditorKind.Choice, Order = 0)]
    public string Language { get; set; } = AppLanguage.System;

    [Setting(SettingsSection.Appearance, SettingEditorKind.Choice, Order = 0)]
    public ThemePreference Theme { get; set; } = ThemePreference.System;

    /// <summary>
    /// 搜索框相对主显示器工作区中央的偏移（DIP），(0, 0) 即默认居中；
    /// 越界时定位阶段会把窗口夹回工作区内。
    /// </summary>
    [Setting(SettingsSection.Appearance, SettingEditorKind.Offset, Order = 1)]
    public ScreenOffset SearchBarOffset { get; set; } = ScreenOffset.Default;

    /// <summary>网页搜索使用的默认搜索引擎，可选 baidu/google/bing，默认 baidu。</summary>
    [Setting(SettingsSection.Search, SettingEditorKind.Choice, Order = 0)]
    public SearchEngineKind SearchEngine { get; set; } = SearchEngineKind.Baidu;

    /// <summary>
    /// 内置模式列表，顺序即 Tab/模式轮盘顺序；Enabled=false 的模式不参与切换但保留在列表中。
    /// 至少启用一个模式；规范化见 <see cref="ModePreferenceNormalizer"/>。
    /// </summary>
    [Setting(SettingsSection.Modes, SettingEditorKind.ModeList, Order = 0)]
    public ModePreference[] ModePreferences { get; set; } = ModePreferenceDefaults.AllEnabled();

    /// <summary>全局呼出/隐藏搜索框快捷键，默认 Alt+Space。</summary>
    [Setting(SettingsSection.Shortcuts, SettingEditorKind.ShortcutKey, Order = 0)]
    public string ToggleVisibilityShortcut { get; set; } =
        ShortcutDefaults.GetDefaultValue(ShortcutAction.ToggleVisibility);

    /// <summary>轻按切换下一模式、长按呼出模式轮盘的单键，默认 Tab。</summary>
    [Setting(SettingsSection.Shortcuts, SettingEditorKind.ShortcutKey, Order = 1)]
    public string CycleModeShortcut { get; set; } =
        ShortcutDefaults.GetDefaultValue(ShortcutAction.CycleMode);


    /// <summary>执行当前项，默认 Enter。</summary>
    [Setting(SettingsSection.Shortcuts, SettingEditorKind.ShortcutKey, Order = 3)]
    public string ExecuteShortcut { get; set; } =
        ShortcutDefaults.GetDefaultValue(ShortcutAction.Execute);

    /// <summary>上移选择（词典候选与模式轮盘），默认 Up。</summary>
    [Setting(SettingsSection.Shortcuts, SettingEditorKind.ShortcutKey, Order = 4)]
    public string SelectPreviousShortcut { get; set; } =
        ShortcutDefaults.GetDefaultValue(ShortcutAction.SelectPrevious);

    /// <summary>下移选择（词典候选与模式轮盘），默认 Down。</summary>
    [Setting(SettingsSection.Shortcuts, SettingEditorKind.ShortcutKey, Order = 5)]
    public string SelectNextShortcut { get; set; } =
        ShortcutDefaults.GetDefaultValue(ShortcutAction.SelectNext);

    /// <summary>搜索框显示时是否在按下 Esc 后隐藏；模式轮盘打开时 Esc 始终先取消轮盘。</summary>
    [Setting(SettingsSection.Shortcuts, SettingEditorKind.Toggle, Order = 6)]
    public bool HideOnEscape { get; set; } = true;

    /// <summary>搜索框隐藏后是否清空已输入内容，默认 true。</summary>
    [Setting(SettingsSection.General, SettingEditorKind.Toggle, Order = 1)]
    public bool ClearQueryOnHide { get; set; } = true;

    /// <summary>登录 Windows 后是否自动启动并静默驻留系统托盘，默认 false。</summary>
    [Setting(SettingsSection.General, SettingEditorKind.Toggle, Order = 2)]
    public bool LaunchAtStartup { get; set; }

    /// <summary>
    /// 全局快捷键呼出搜索框前，是否跳过当前前台的全屏应用，默认 true。
    /// 托盘点击与手动启动不受该设置影响。
    /// </summary>
    [Setting(SettingsSection.General, SettingEditorKind.Toggle, Order = 3)]
    public bool SkipWhenFullscreenAppActive { get; set; } = true;

    /// <summary>询问 DeepSeek 网页端地址模板，{0} 将被替换为 URL 编码后的查询词。</summary>
    [Setting(
        SettingsSection.Search,
        SettingEditorKind.Text,
        Order = 1,
        Validation = SettingValidation.UrlTemplate)]
    public string AskAiUrlTemplate { get; set; } =
        "https://chat.deepseek.com/?q={0}";
    /// <summary>
    /// 汉英词典文件路径：.txt（CC-CEDICT 文本或“词头+释义”行文本）、.csv、.gz/.zip 或 StarDict（.ifo）；
    /// 留空时依次使用用户数据目录、程序目录中的同名文件，最后使用随程序内嵌的内置 CC-CEDICT。
    /// </summary>
    [Setting(
        SettingsSection.Dictionary,
        SettingEditorKind.FilePath,
        Order = 0,
        FileTypeFilterKey = SettingFileFilterKeys.CcCedict,
        FileTypePatterns = new[] { "*.txt", "*.csv", "*.gz", "*.zip", "*.ifo", "*" })]
    public string? DictionaryFilePath { get; set; }

    /// <summary>
    /// 英汉词典文件路径：.txt（“词头+释义”行文本）、.csv、.gz/.zip 或 StarDict（.ifo）；
    /// 留空时依次使用用户数据目录、程序目录中的同名文件，最后使用随程序内嵌的内置 ECDICT。
    /// </summary>
    [Setting(
        SettingsSection.Dictionary,
        SettingEditorKind.FilePath,
        Order = 1,
        FileTypeFilterKey = SettingFileFilterKeys.Ecdict,
        FileTypePatterns = new[] { "*.txt", "*.csv", "*.gz", "*.zip", "*.ifo", "*" })]
    public string? EcdictFilePath { get; set; }
}
