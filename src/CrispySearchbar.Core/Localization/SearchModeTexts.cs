namespace CrispySearchbar.Core.Localization;

/// <summary>一种搜索模式在某个语言下的标题、占位提示与操作提示。</summary>
public sealed record SearchModeTexts(
    string Title,
    string Watermark,
    string ActionHint);
