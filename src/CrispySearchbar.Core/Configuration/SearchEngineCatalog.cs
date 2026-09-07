namespace CrispySearchbar.Core.Configuration;

/// <summary>内置搜索引擎与 URL 模板的映射；配置选择使用这里定义的名字。</summary>
public static class SearchEngineCatalog
{
    public static IReadOnlyList<SearchEngineKind> BuiltIn { get; } =
        new[] { SearchEngineKind.Baidu, SearchEngineKind.Google, SearchEngineKind.Bing };

    public static string GetUrlTemplate(SearchEngineKind engine) => engine switch
    {
        SearchEngineKind.Baidu => "https://www.baidu.com/s?wd={0}",
        SearchEngineKind.Google => "https://www.google.com/search?q={0}",
        SearchEngineKind.Bing => "https://www.bing.com/search?q={0}",
        _ => "https://www.baidu.com/s?wd={0}",
    };

    public static string GetDisplayName(SearchEngineKind engine) => engine switch
    {
        SearchEngineKind.Baidu => "百度",
        SearchEngineKind.Google => "Google",
        SearchEngineKind.Bing => "必应",
        _ => engine.ToString(),
    };
}
