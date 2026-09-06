using CrispySearchbar.Core.Configuration;

namespace CrispySearchbar.Core.Modes;

/// <summary>默认搜索模式列表，Tab 按此顺序循环。</summary>
public static class SearchModeCatalog
{
    public static IReadOnlyList<SearchMode> CreateDefault(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new SearchMode[]
        {
            SearchMode.WebSearch(settings.SearchEngineUrlTemplate),
            SearchMode.AskAi(settings.AskAiUrlTemplate),
            SearchMode.Dictionary,
        };
    }
}
