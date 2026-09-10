using System.IO.Compression;

namespace CrispySearchbar.Core.Dictionary;

/// <summary>
/// 从磁盘加载 StarDict 词库并构建现有索引。
/// 在后台线程全量解码词条正文，再按方向映射为 <see cref="DictionaryEntry"/>（汉英）或
/// <see cref="EcdictEntry"/>（英汉），复用 <see cref="CedictIndex"/>/<see cref="EcdictIndex"/> 的匹配与排序。
/// </summary>
public static class StarDictDictionaryLoader
{
    private const int MaxSensesPerEntry = 64;

    public static CedictIndex LoadChineseIndex(string ifoFilePath)
        => CedictIndex.Build(BuildChineseEntries(ifoFilePath));

    public static EcdictIndex LoadEnglishIndex(string ifoFilePath)
        => EcdictIndex.Build(BuildEnglishEntries(ifoFilePath));

    /// <summary>把 StarDict 词条展开为方向无关记录（同义词各成一条），来源名取 .ifo 的 bookname。</summary>
    public static IReadOnlyList<DictionaryRecord> BuildRecords(string ifoFilePath)
    {
        var (entries, sourceName) = LoadEntries(ifoFilePath);
        var records = new List<DictionaryRecord>(entries.Count);
        foreach (var entry in entries)
        {
            // 拼音优先取 y 字段，缺失时退回 t 字段（音标）。
            var pinyin = entry.Pinyin ?? entry.Phonetic;
            foreach (var headword in entry.Headwords)
            {
                records.Add(new DictionaryRecord(
                    headword,
                    Traditional: null,
                    pinyin,
                    entry.Phonetic,
                    entry.Definitions,
                    Exchange: null,
                    sourceName));
            }
        }

        return records;
    }

    /// <summary>汉英方向：词头同时作为简体与繁体，拼音取 y/t 字段。</summary>
    public static IReadOnlyList<DictionaryEntry> BuildChineseEntries(string ifoFilePath)
        => BuildRecords(ifoFilePath)
            .Select(DictionaryRecordMapper.ToDictionaryEntry)
            .ToList();

    /// <summary>英汉方向：音标取 t 字段，义项复用文本字段。</summary>
    public static IReadOnlyList<EcdictEntry> BuildEnglishEntries(string ifoFilePath)
        => BuildRecords(ifoFilePath)
            .Select(DictionaryRecordMapper.ToEcdictEntry)
            .ToList();

    private static (List<StarDictEntry> Entries, string SourceName) LoadEntries(string ifoFilePath)
    {
        var info = StarDictIfoParser.Parse(ifoFilePath);
        var indexEntries = StarDictIndexReader.ReadIndex(
            File.ReadAllBytes(info.IdxFilePath),
            info.IdxOffsetBits);
        var data = ReadDataBytes(info.DataFilePath);

        // 每个 .idx 记录先放自己的词头，再把同义词表指向它的词头并入。
        var headwords = new List<List<string>>(indexEntries.Count);
        foreach (var indexEntry in indexEntries)
        {
            headwords.Add([indexEntry.Word]);
        }

        if (info.SynFilePath is not null && headwords.Count > 0)
        {
            foreach (var (word, target) in StarDictIndexReader.ReadSynonyms(
                File.ReadAllBytes(info.SynFilePath)))
            {
                if (target >= 0 && target < headwords.Count)
                {
                    headwords[target].Add(word);
                }
            }
        }

        var entries = new List<StarDictEntry>(indexEntries.Count);
        for (var i = 0; i < indexEntries.Count; i++)
        {
            var indexEntry = indexEntries[i];
            if (indexEntry.Offset < 0
                || indexEntry.Size <= 0
                || indexEntry.Offset + indexEntry.Size > data.LongLength)
            {
                continue;
            }

            var fields = StarDictEntryDecoder.Decode(
                data.AsSpan((int)indexEntry.Offset, indexEntry.Size),
                info.SametypeSequence);
            var decoded = ExtractFields(fields);
            if (decoded.Definitions.Count == 0)
            {
                continue;
            }

            entries.Add(new StarDictEntry(
                headwords[i],
                decoded.Phonetic,
                decoded.Pinyin,
                decoded.Definitions));
        }

        return (entries, info.SourceName);
    }

    private static (string? Phonetic, string? Pinyin, IReadOnlyList<string> Definitions) ExtractFields(
        IReadOnlyList<StarDictField> fields)
    {
        string? phonetic = null;
        string? pinyin = null;
        var definitions = new List<string>();
        foreach (var field in fields)
        {
            var plainText = StarDictMarkup.ToPlainText(field.Text);
            switch (field.Type)
            {
                case 't':
                    phonetic ??= plainText;
                    break;
                case 'y':
                    pinyin ??= plainText;
                    break;
                default:
                    if (!StarDictEntryDecoder.IsTextType(field.Type)
                        || definitions.Count >= MaxSensesPerEntry)
                    {
                        break;
                    }

                    foreach (var line in StarDictMarkup.SplitLines(plainText))
                    {
                        if (definitions.Count >= MaxSensesPerEntry)
                        {
                            break;
                        }

                        definitions.Add(line);
                    }

                    break;
            }
        }

        return (phonetic, pinyin, definitions);
    }

    /// <summary>读取 .dict；.dict.dz（dictzip 为 gzip 兼容格式）用 GZipStream 全量解压。</summary>
    private static byte[] ReadDataBytes(string dataFilePath)
    {
        if (dataFilePath.EndsWith(".dz", StringComparison.OrdinalIgnoreCase))
        {
            using var file = File.OpenRead(dataFilePath);
            using var gzip = new GZipStream(file, CompressionMode.Decompress);
            using var buffer = new MemoryStream();
            gzip.CopyTo(buffer);
            return buffer.ToArray();
        }

        return File.ReadAllBytes(dataFilePath);
    }

    private sealed record StarDictEntry(
        IReadOnlyList<string> Headwords,
        string? Phonetic,
        string? Pinyin,
        IReadOnlyList<string> Definitions);
}
