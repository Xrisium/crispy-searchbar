namespace CrispySearchbar.Core.Modes;

/// <summary>一种可切换的搜索模式。</summary>
public sealed record SearchMode(
    string Key,
    string Title,
    string Watermark,
    string ActionHint,
    string? UrlTemplate)
{
    public static SearchMode WebSearch(string urlTemplate)
        => new("web-search", "网页搜索", "输入关键词，按 Enter 搜索",
            "按 Enter 使用默认搜索引擎打开", urlTemplate);

    public static SearchMode AskAi(string urlTemplate)
        => new("ask-ai", "问问大肥鱼", "输入问题，按 Enter 跳转到 DeepSeek 网页端",
            "按 Enter 跳转到 DeepSeek 网页端", urlTemplate);

    public static SearchMode Dictionary { get; } = new(
        "dictionary", "词典", "输入英文单词或中文词语",
        "输入后实时查词，Enter 查看释义", UrlTemplate: null);
}
