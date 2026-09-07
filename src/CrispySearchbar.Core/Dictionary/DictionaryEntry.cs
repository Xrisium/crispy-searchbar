namespace CrispySearchbar.Core.Dictionary;

/// <summary>词典模式中的一条词条。字段语义以 CC-CEDICT 数据为准。</summary>
public sealed record DictionaryEntry(
    string Traditional,
    string Simplified,
    string Pinyin,
    IReadOnlyList<string> Definitions)
{
    /// <summary>来源词典标识，为将来多词典合并预留。</summary>
    public string Source { get; init; } = CedictParser.SourceName;
}
