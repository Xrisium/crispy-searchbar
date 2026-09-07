using CrispySearchbar.Core.Localization;

namespace CrispySearchbar.Core.Configuration;

/// <summary>应用设置。新增字段必须提供可直接运行的默认值。</summary>
public sealed class AppSettings
{
    /// <summary>
    /// 界面语言代码（BCP 47），当前支持 zh-Hans/en，默认 zh-Hans。
    /// 未知值由 AppStrings.For 回退到默认简体中文。
    /// </summary>
    [Setting(SettingsSection.General, SettingEditorKind.Choice, Order = 0)]
    [SettingValues(Values = new[] { AppLanguage.SimplifiedChinese, AppLanguage.English })]
    public string Language { get; set; } = AppLanguage.SimplifiedChinese;

    [Setting(SettingsSection.Appearance, SettingEditorKind.Choice, Order = 0)]
    public ThemePreference Theme { get; set; } = ThemePreference.System;

    /// <summary>网页搜索使用的默认搜索引擎，可选 baidu/google/bing，默认 baidu。</summary>
    [Setting(SettingsSection.Search, SettingEditorKind.Choice, Order = 0)]
    public SearchEngineKind SearchEngine { get; set; } = SearchEngineKind.Baidu;

    /// <summary>搜索框隐藏后是否清空已输入内容，默认 true。</summary>
    [Setting(SettingsSection.General, SettingEditorKind.Toggle, Order = 1)]
    public bool ClearQueryOnHide { get; set; } = true;

    /// <summary>问问大肥鱼（DeepSeek 网页端）地址模板，{0} 将被替换为 URL 编码后的查询词。</summary>
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
