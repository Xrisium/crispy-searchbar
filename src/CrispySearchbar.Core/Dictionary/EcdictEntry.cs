namespace CrispySearchbar.Core.Dictionary;

/// <summary>ECDICT（英→汉）词条：英文词头与中文释义等。</summary>
public sealed record EcdictEntry(
    string Word,
    string? Phonetic,
    IReadOnlyList<string> Senses,
    string? Exchange)
{
    /// <summary>来源词典标识，为将来多词典合并预留。</summary>
    public string Source { get; init; } = EcdictParser.SourceName;
}
