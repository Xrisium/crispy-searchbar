using CrispySearchbar.Core.Configuration;
using CrispySearchbar.Core.Localization;

namespace CrispySearchbar.Core.Modes;

/// <summary>默认搜索模式列表，Tab 按此顺序循环。</summary>
public static class SearchModeCatalog
{
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
            SearchMode.AskAi(strings.AskAiMode, settings.AskAiUrlTemplate),
            SearchMode.Dictionary(strings.DictionaryMode),
        };
    }
}
