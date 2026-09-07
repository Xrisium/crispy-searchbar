using CrispySearchbar.Core.Dictionary;

namespace CrispySearchbar.ViewModels;

/// <summary>汉→英方向（CC-CEDICT）的候选/详情模型。</summary>
public sealed class ChineseDictionaryCandidateViewModel : DictionaryCandidateViewModel
{
    private readonly DictionaryEntry _entry;

    public ChineseDictionaryCandidateViewModel(DictionaryEntry entry)
    {
        _entry = entry;
    }

    public DictionaryEntry Entry => _entry;

    public override string HeadwordLine => ChineseHeadwordLine;

    public override string DetailLine => JoinParts(
        _entry.Pinyin,
        _entry.Definitions.Count > 0 ? _entry.Definitions[0] : null);

    public override string DetailTitle => _entry.Simplified;

    public override string DetailSubtitle => JoinParts(
        string.Equals(_entry.Traditional, _entry.Simplified, StringComparison.Ordinal)
            ? null
            : _entry.Traditional,
        _entry.Pinyin);

    public override IReadOnlyList<string> Definitions => _entry.Definitions;

    public override string Source => _entry.Source;

    public string ChineseHeadwordLine =>
        string.Equals(_entry.Traditional, _entry.Simplified, StringComparison.Ordinal)
            ? _entry.Simplified
            : $"{_entry.Simplified} / {_entry.Traditional}";
}
