using CrispySearchbar.Core.Dictionary;

namespace CrispySearchbar.ViewModels;

/// <summary>候选列表中一行词条的显示模型。</summary>
public sealed class DictionaryCandidateViewModel
{
    public DictionaryCandidateViewModel(DictionaryEntry entry)
    {
        Entry = entry;

        HeadwordLine = string.Equals(entry.Traditional, entry.Simplified, StringComparison.Ordinal)
            ? entry.Simplified
            : $"{entry.Simplified} / {entry.Traditional}";

        var subtitleParts = new List<string>(2);
        if (!string.IsNullOrWhiteSpace(entry.Pinyin))
        {
            subtitleParts.Add(entry.Pinyin);
        }

        if (entry.Definitions.Count > 0)
        {
            subtitleParts.Add(entry.Definitions[0]);
        }

        DetailLine = string.Join(" · ", subtitleParts);
    }

    public DictionaryEntry Entry { get; }

    public string HeadwordLine { get; }

    public string DetailLine { get; }
}
