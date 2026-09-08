using System.Text.Json.Serialization;
using CrispySearchbar.Core.Configuration;

namespace CrispySearchbar.Core.Localization;

/// <summary>
/// 一种语言下用户可见文案的强类型集合。
/// 值由 <see cref="TranslationCatalog"/> 从 locales/*.json 读取并填充，
/// 缺失词条继承英文；Language 与文件元数据也由加载器写入。
/// </summary>
public sealed class AppStrings
{
    /// <summary>本实例的语言代码，取自翻译文件名（如 zh-Hans）。</summary>
    public required string Language { get; init; }

    /// <summary>本语言在其自身语言中的显示名（用于设置中的语言列表）。</summary>
    public required string NativeName { get; init; }

    /// <summary>该界面语言对应的 Wikipedia 站点语言段，如 en、zh。</summary>
    [JsonPropertyName("wikipediaLanguage")]
    public required string WikipediaLanguageCode { get; init; }

    public required SearchModeTexts WebSearchMode { get; init; }

    public required SearchModeTexts WikipediaMode { get; init; }

    public required SearchModeTexts AskAiMode { get; init; }

    public required SearchModeTexts DictionaryMode { get; init; }

    public required string TrayToolTip { get; init; }

    public required string ShowHideSearchBar { get; init; }

    public required string OpenSettings { get; init; }

    public required string Exit { get; init; }

    public required AppSettingsTexts SettingsTexts { get; init; }

    public required string DictionaryEmptyHint { get; init; }

    public required string DictionaryLoadingHint { get; init; }

    public required string DictionaryDataSourceNotConfigured { get; init; }

    public required string DictionaryNoResultsTemplate { get; init; }

    public required string DictionaryLoadFailedTemplate { get; init; }

    public required string DictionaryReadFailedTemplate { get; init; }

    public required string ConfiguredDictionaryFileMissingTemplate { get; init; }

    public required string DictionaryDataFileNotFoundTemplate { get; init; }

    public string FormatDictionaryNoResults(string query)
        => string.Format(DictionaryNoResultsTemplate, query);

    public string FormatDictionaryLoadFailed(string? detail)
        => string.Format(DictionaryLoadFailedTemplate, detail);

    public string FormatDictionaryReadFailed(string source, string? detail)
        => string.Format(DictionaryReadFailedTemplate, source, detail);

    public string FormatConfiguredDictionaryFileMissing(string source, string path)
        => string.Format(ConfiguredDictionaryFileMissingTemplate, source, path);

    public string FormatDictionaryDataFileNotFound(
        string source,
        string fileName,
        string userDataFilePath,
        string bundledFilePath,
        string downloadUrl)
        => string.Format(
            DictionaryDataFileNotFoundTemplate,
            source,
            fileName,
            userDataFilePath,
            bundledFilePath,
            downloadUrl);

    public string GetSearchEngineDisplayName(SearchEngineKind engine)
        => SettingsTexts.GetOptionLabel(nameof(AppSettings.SearchEngine), engine.ToString());
}
