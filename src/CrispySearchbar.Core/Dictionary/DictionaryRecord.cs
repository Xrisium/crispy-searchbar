namespace CrispySearchbar.Core.Dictionary;

/// <summary>
/// 与查询方向无关的词条记录：各格式解析器只产出它，
/// 再由调用方按槽位映射为 <see cref="DictionaryEntry"/>（汉英）或 <see cref="EcdictEntry"/>（英汉）。
/// </summary>
public sealed record DictionaryRecord(
    string Headword,
    string? Traditional,
    string? Pinyin,
    string? Phonetic,
    IReadOnlyList<string> Definitions,
    string? Exchange,
    string Source);

/// <summary>把方向无关的 <see cref="DictionaryRecord"/> 映射为两个方向的词条模型。</summary>
public static class DictionaryRecordMapper
{
    public static DictionaryEntry ToDictionaryEntry(DictionaryRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        return new DictionaryEntry(
            record.Traditional ?? record.Headword,
            record.Headword,
            record.Pinyin ?? string.Empty,
            record.Definitions)
        {
            Source = record.Source,
        };
    }

    public static EcdictEntry ToEcdictEntry(DictionaryRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        return new EcdictEntry(
            record.Headword,
            record.Phonetic,
            record.Definitions,
            record.Exchange)
        {
            Source = record.Source,
        };
    }
}
