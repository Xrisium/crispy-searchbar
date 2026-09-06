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
        => new("web-search", "网页搜索", "输入关键词，按 Enter 打开搜索结果",
            "按 Enter 使用搜索引擎打开", urlTemplate);

    public static SearchMode AskAi(string urlTemplate)
        => new("ask-ai", "询问 AI", "输入完整问题，按 Enter 询问 DeepSeek",
            "按 Enter 在浏览器中打开 DeepSeek", urlTemplate);

    public static SearchMode Dictionary { get; } = new(
        "dictionary", "词典", "输入英文单词或中文词语（词典模式开发中）",
        "词典模式即将支持", UrlTemplate: null);
}
