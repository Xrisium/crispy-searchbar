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

    /// <summary>网页搜索使用的默认搜索引擎，可选 baidu/google/bing，默认 baidu。</summary>
    [Setting(SettingsSection.Search, SettingEditorKind.Choice, Order = 0)]
    public SearchEngineKind SearchEngine { get; set; } = SearchEngineKind.Baidu;

    /// <summary>
    /// 内置模式列表，顺序即 Tab/模式轮盘顺序；Enabled=false 的模式不参与切换但保留在列表中。
    /// 至少启用一个模式；规范化见 <see cref="ModePreferenceNormalizer"/>。
    /// </summary>
    [Setting(SettingsSection.Modes, SettingEditorKind.ModeList, Order = 0)]
    public ModePreference[] ModePreferences { get; set; } = ModePreferenceDefaults.AllEnabled();

    /// <summary>搜索框隐藏后是否清空已输入内容，默认 true。</summary>
    [Setting(SettingsSection.General, SettingEditorKind.Toggle, Order = 1)]
    public bool ClearQueryOnHide { get; set; } = true;

    /// <summary>询问 DeepSeek 网页端地址模板，{0} 将被替换为 URL 编码后的查询词。</summary>
    [Setting(
        SettingsSection.Search,
        SettingEditorKind.Text,
        Order = 1,
        Validation = SettingValidation.UrlTemplate)]
    public string AskAiUrlTemplate { get; set; } =
        "https://chat.deepseek.com/?q={0}";
    /// <summary>
    /// CC-CEDICT（汉英）词典文件路径；留空时依次使用用户数据目录、程序目录中的 cedict_ts.u8。
    /// </summary>
    [Setting(
        SettingsSection.Dictionary,
        SettingEditorKind.FilePath,
        Order = 0,
        FileTypeFilterKey = SettingFileFilterKeys.CcCedict,
        FileTypePatterns = new[] { "*.u8", "*" })]
    public string? DictionaryFilePath { get; set; }

    /// <summary>
    /// ECDICT（英汉）词典文件路径；留空时依次使用用户数据目录、程序目录中的 ecdict.csv。
    /// </summary>
    [Setting(
        SettingsSection.Dictionary,
        SettingEditorKind.FilePath,
        Order = 1,
        FileTypeFilterKey = SettingFileFilterKeys.Ecdict,
        FileTypePatterns = new[] { "*.csv", "*" })]
    public string? EcdictFilePath { get; set; }
}
