using CrispySearchbar.Core.Dictionary;

namespace CrispySearchbar.ViewModels;

/// <summary>
/// 候选列表中一行词条的显示模型。
/// 英文查询按英-汉方向展示：主行是英文词条，副行是中文对应词与拼音。
/// 中文查询保持汉-英方向展示。
/// </summary>
public sealed class DictionaryCandidateViewModel
{
    private readonly DictionarySearchHit _hit;

    public DictionaryCandidateViewModel(DictionarySearchHit hit)
    {
        _hit = hit;
    }

    public DictionaryEntry Entry => _hit.Entry;

    public bool IsEnglishMatch => _hit.IsEnglishMatch;

    /// <summary>英文查询命中的英文词形；中文查询为 null。</summary>
    public string? EnglishForm => _hit.EnglishForm;

    /// <summary>主行：英文方向显示英文词条，中文方向显示简体/繁体词头。</summary>
    public string HeadwordLine => IsEnglishMatch
        ? (EnglishForm ?? Entry.Simplified)
        : ChineseHeadwordLine;

    /// <summary>副行：英文方向显示中文词头与拼音；中文方向显示拼音与首条释义。</summary>
    public string DetailLine
    {
        get
        {
            if (IsEnglishMatch)
            {
                return string.Join(" · ", Parts(ChineseHeadwordLine, Entry.Pinyin));
            }

            var parts = new List<string>(2);
            if (!string.IsNullOrWhiteSpace(Entry.Pinyin))
            {
                parts.Add(Entry.Pinyin);
            }

            if (Entry.Definitions.Count > 0)
            {
                parts.Add(Entry.Definitions[0]);
            }

            return string.Join(" · ", parts);
        }
    }

    public string ChineseHeadwordLine =>
        string.Equals(Entry.Traditional, Entry.Simplified, StringComparison.Ordinal)
            ? Entry.Simplified
            : $"{Entry.Simplified} / {Entry.Traditional}";

    private static List<string> Parts(params string?[] values)
    {
        var parts = new List<string>(values.Length);
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                parts.Add(value);
            }
        }

        return parts;
    }
}
