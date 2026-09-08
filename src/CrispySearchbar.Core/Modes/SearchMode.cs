using CrispySearchbar.Core.Localization;

namespace CrispySearchbar.Core.Modes;

/// <summary>一种可切换的搜索模式。显示文案由调用方按当前语言传入。</summary>
public sealed record SearchMode(
    string Key,
    string Title,
    string Watermark,
    string ActionHint,
    string? UrlTemplate)
{
    public static SearchMode WebSearch(SearchModeTexts texts, string urlTemplate)
        => new("web-search", texts.Title, texts.Watermark, texts.ActionHint, urlTemplate);

    public static SearchMode AskAi(SearchModeTexts texts, string urlTemplate)
        => new("ask-ai", texts.Title, texts.Watermark, texts.ActionHint, urlTemplate);

    public static SearchMode Wikipedia(SearchModeTexts texts, string urlTemplate)
        => new("wikipedia", texts.Title, texts.Watermark, texts.ActionHint, urlTemplate);

    public static SearchMode Dictionary(SearchModeTexts texts)
        => new("dictionary", texts.Title, texts.Watermark, texts.ActionHint, UrlTemplate: null);
}
