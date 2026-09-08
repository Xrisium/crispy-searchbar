using System.Text.RegularExpressions;
using CrispySearchbar.Core.Configuration;
using CrispySearchbar.Core.Localization;

namespace CrispySearchbar.Core.Modes;

/// <summary>内置搜索模式目录；Tab 与模式轮盘按用户配置的顺序与启用状态生成。</summary>
public static class SearchModeCatalog
{
    private static readonly Regex WikipediaLanguagePattern = new(
        "^[a-z]{2,8}(-[a-z0-9]{1,8})*$",
        RegexOptions.CultureInvariant);

    /// <summary>
    /// 维基百科目标站点语言跟随各翻译文件的 wikipediaLanguage 元数据；
    /// 缺失或非法值回退英文站点。
    /// </summary>
    public static string GetWikipediaUrlTemplate(AppStrings strings)
    {
        ArgumentNullException.ThrowIfNull(strings);
        var language = NormalizeWikipediaLanguage(strings.WikipediaLanguageCode);
        return $"https://{language}.wikipedia.org/w/index.php?search={{0}}";
    }

    public static IReadOnlyList<SearchMode> Create(
        AppSettings settings,
        AppStrings strings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(strings);

        var shortcuts = ShortcutCatalog.Create(settings);
        var modesByKey = new Dictionary<string, SearchMode>(StringComparer.Ordinal)
        {
            [ModePreferenceDefaults.WebSearch] = SearchMode.WebSearch(
                ShortcutTextFormatter.Format(strings.WebSearchMode, shortcuts),
                SearchEngineCatalog.GetUrlTemplate(settings.SearchEngine)),
            [ModePreferenceDefaults.Wikipedia] = SearchMode.Wikipedia(
                ShortcutTextFormatter.Format(strings.WikipediaMode, shortcuts),
                GetWikipediaUrlTemplate(strings)),
            [ModePreferenceDefaults.AskAi] = SearchMode.AskAi(
                ShortcutTextFormatter.Format(strings.AskAiMode, shortcuts),
                settings.AskAiUrlTemplate),
            [ModePreferenceDefaults.Dictionary] = SearchMode.Dictionary(
                ShortcutTextFormatter.Format(strings.DictionaryMode, shortcuts)),
        };

        var preferences = ModePreferenceNormalizer.Normalize(settings.ModePreferences);
        return preferences
            .Where(preference => preference.Enabled)
            .Select(preference => modesByKey[preference.Key])
            .ToArray();
    }

    private static string NormalizeWikipediaLanguage(string? code)
    {
        var candidate = string.IsNullOrWhiteSpace(code)
            ? AppLanguage.English
            : code.Trim().ToLowerInvariant();
        return WikipediaLanguagePattern.IsMatch(candidate)
            ? candidate
            : AppLanguage.English;
    }
}
