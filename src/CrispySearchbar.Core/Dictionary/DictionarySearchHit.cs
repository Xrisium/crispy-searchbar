namespace CrispySearchbar.Core.Dictionary;

/// <summary>
/// 一次词典搜索结果。英文查询返回英-汉方向的命中，
/// <see cref="EnglishForm"/> 为匹配到的英文词形；中文查询不设该值。
/// </summary>
public sealed record DictionarySearchHit(DictionaryEntry Entry, string? EnglishForm)
{
    public bool IsEnglishMatch => !string.IsNullOrWhiteSpace(EnglishForm);
}
