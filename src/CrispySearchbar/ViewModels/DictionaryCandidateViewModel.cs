namespace CrispySearchbar.ViewModels;

/// <summary>
/// 词典候选行/详情的数据模型基类：
/// 汉英查询使用 CC-CEDICT 词条，英汉查询使用 ECDICT 词条。
/// </summary>
public abstract class DictionaryCandidateViewModel
{
    /// <summary>候选主行：中文词头（汉英）或英文词条（英汉）。</summary>
    public abstract string HeadwordLine { get; }

    /// <summary>候选副行：拼音/音标与首条释义的摘要。</summary>
    public abstract string DetailLine { get; }

    /// <summary>详情主标题。</summary>
    public abstract string DetailTitle { get; }

    /// <summary>详情副标题（繁体、拼音或音标），可为空。</summary>
    public abstract string DetailSubtitle { get; }

    /// <summary>详情释义列表。</summary>
    public abstract IReadOnlyList<string> Definitions { get; }

    /// <summary>来源词典标识。</summary>
    public abstract string Source { get; }

    protected static string JoinParts(params string?[] values)
    {
        var parts = new List<string>(values.Length);
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                parts.Add(value);
            }
        }

        return string.Join(" · ", parts);
    }
}
