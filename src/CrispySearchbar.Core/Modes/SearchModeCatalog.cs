using CrispySearchbar.Core.Configuration;
using CrispySearchbar.Core.Localization;

namespace CrispySearchbar.Core.Modes;

/// <summary>默认搜索模式列表，Tab 按此顺序循环。</summary>
public static class SearchModeCatalog
{
    private const string ChineseWikipediaUrlTemplate =
        "https://zh.wikipedia.org/w/index.php?search={0}";

    private const string EnglishWikipediaUrlTemplate =
        "https://en.wikipedia.org/w/index.php?search={0}";

    /// <summary>维基百科模式的目标站点语言跟随界面语言。</summary>
    public static string GetWikipediaUrlTemplate(AppStrings strings)
    {
        ArgumentNullException.ThrowIfNull(strings);
        return string.Equals(
            strings.Language,
            AppLanguage.SimplifiedChinese,
            StringComparison.OrdinalIgnoreCase)
            ? ChineseWikipediaUrlTemplate
            : EnglishWikipediaUrlTemplate;
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
}
