using System.Text.RegularExpressions;
using CrispySearchbar.Core.Configuration;
using CrispySearchbar.Core.Localization;

namespace CrispySearchbar.Core.Modes;

/// <summary>默认搜索模式列表，Tab 按此顺序循环。</summary>
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

    public static IReadOnlyList<SearchMode> CreateDefault(
        AppSettings settings,
        AppStrings strings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(strings);

        return new SearchMode[]
        {
            SearchMode.WebSearch(
                strings.WebSearchMode,
                SearchEngineCatalog.GetUrlTemplate(settings.SearchEngine)),
            SearchMode.Wikipedia(
                strings.WikipediaMode,
                GetWikipediaUrlTemplate(strings)),
            SearchMode.AskAi(strings.AskAiMode, settings.AskAiUrlTemplate),
            SearchMode.Dictionary(strings.DictionaryMode),
        };
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
