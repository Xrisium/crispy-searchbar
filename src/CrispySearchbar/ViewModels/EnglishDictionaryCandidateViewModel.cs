using CrispySearchbar.Core.Dictionary;

namespace CrispySearchbar.ViewModels;

/// <summary>英→汉方向（ECDICT）的候选/详情模型。</summary>
public sealed class EnglishDictionaryCandidateViewModel : DictionaryCandidateViewModel
{
    private readonly EcdictEntry _entry;

    public EnglishDictionaryCandidateViewModel(EcdictEntry entry)
    {
        _entry = entry;
    }

    public EcdictEntry Entry => _entry;

    public override string HeadwordLine => _entry.Word;

    public override string DetailLine => JoinParts(
        _entry.Phonetic,
        _entry.Senses.Count > 0 ? _entry.Senses[0] : null);

    public override string DetailTitle => _entry.Word;

    public override string DetailSubtitle => _entry.Phonetic ?? string.Empty;

    public override IReadOnlyList<string> Definitions => _entry.Senses;

    public override string Source => _entry.Source;
}
