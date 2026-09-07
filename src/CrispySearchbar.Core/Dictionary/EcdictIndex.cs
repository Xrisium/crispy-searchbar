using System.Text;

namespace CrispySearchbar.Core.Dictionary;

/// <summary>
/// ECDICT（英→汉）不可变内存索引：按英文词头排序，
/// 支持精确、前缀与子串补位查询。
/// </summary>
public sealed class EcdictIndex
{
    private const int MaxRawCandidatesPerQuery = 800;

    private readonly EcdictEntry[] _entries;
    private readonly WordRef[] _wordRefs;
    private readonly Dictionary<string, int[]> _exactWords;

    private EcdictIndex(
        EcdictEntry[] entries,
        WordRef[] wordRefs,
        Dictionary<string, int[]> exactWords)
    {
        _entries = entries;
        _wordRefs = wordRefs;
        _exactWords = exactWords;
    }

    public int Count => _entries.Length;

    public IReadOnlyList<EcdictEntry> Entries => _entries;

    public static EcdictIndex Empty { get; } = new(
        [],
        [],
        new Dictionary<string, int[]>());

    public static EcdictIndex Build(IEnumerable<EcdictEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var entryList = entries
            .Where(entry => entry is not null
                && !string.IsNullOrWhiteSpace(entry.Word)
                && entry.Senses.Count > 0)
            .ToArray();
        if (entryList.Length == 0)
        {
            return Empty;
        }

        var wordRefs = new List<WordRef>(entryList.Length);
        var exactBuckets = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        for (var entryIndex = 0; entryIndex < entryList.Length; entryIndex++)
        {
            var normalized = NormalizeLatinText(entryList[entryIndex].Word);
            if (normalized.Length == 0)
            {
                continue;
            }

            wordRefs.Add(new WordRef(normalized, entryIndex));
            if (!exactBuckets.TryGetValue(normalized, out var bucket))
            {
                bucket = [];
                exactBuckets.Add(normalized, bucket);
            }

            bucket.Add(entryIndex);
        }

        wordRefs.Sort(static (left, right) =>
        {
            var byWord = string.CompareOrdinal(left.Normalized, right.Normalized);
            return byWord != 0 ? byWord : left.Entry.CompareTo(right.Entry);
        });

        var exactWords = exactBuckets.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.ToArray(),
            StringComparer.Ordinal);

        return new EcdictIndex(entryList, wordRefs.ToArray(), exactWords);
    }

    /// <summary>
    /// 英文查询（英→汉）：英文词头精确 &gt; 前缀 &gt; 词内子串补位。
    /// 英文大小写与结尾标点不影响匹配。
    /// </summary>
    public IReadOnlyList<EcdictEntry> Search(string? rawQuery, int maxResults = 12)
    {
        if (string.IsNullOrWhiteSpace(rawQuery) || maxResults <= 0)
        {
            return [];
        }

        var query = NormalizeLatinText(rawQuery);
        if (query.Length == 0)
        {
            return [];
        }

        var seen = new HashSet<int>();
        var results = new List<ScoredEntry>(MaxRawCandidatesPerQuery);

        // 精确命中英文词头（可能同词形多个条目）。
        if (_exactWords.TryGetValue(query, out var exactEntries))
        {
            foreach (var entryIndex in exactEntries)
            {
                if (results.Count >= MaxRawCandidatesPerQuery)
                {
                    break;
                }

                if (seen.Add(entryIndex))
                {
                    results.Add(new ScoredEntry(entryIndex, 0));
                }
            }
        }

        // 前缀命中：apple → apple/applepie/Apple Inc. 等。
        if (results.Count < MaxRawCandidatesPerQuery)
        {
            var lower = LowerBound(_wordRefs, query);
            for (var i = lower;
                 i < _wordRefs.Length && results.Count < MaxRawCandidatesPerQuery;
                 i++)
            {
                var wordRef = _wordRefs[i];
                if (!wordRef.Normalized.StartsWith(query, StringComparison.Ordinal))
                {
                    break;
                }

                if (seen.Add(wordRef.Entry))
                {
                    results.Add(new ScoredEntry(wordRef.Entry, 1));
                }
            }
        }

        // 子串补位：仅在结果不足时扫描，避免常见输入被噪声词淹没。
        if (results.Count < maxResults)
        {
            for (var i = 0; i < _wordRefs.Length && results.Count < maxResults; i++)
            {
                var wordRef = _wordRefs[i];
                if (seen.Contains(wordRef.Entry))
                {
                    continue;
                }

                if (wordRef.Normalized.Contains(query, StringComparison.Ordinal))
                {
                    results.Add(new ScoredEntry(wordRef.Entry, 2));
                    seen.Add(wordRef.Entry);
                }
            }
        }

        return Resolve(results, maxResults);
    }

    private IReadOnlyList<EcdictEntry> Resolve(List<ScoredEntry> scored, int maxResults)
    {
        scored.Sort(static (left, right) =>
        {
            var byScore = left.Score.CompareTo(right.Score);
            return byScore != 0 ? byScore : left.Entry.CompareTo(right.Entry);
        });

        var count = Math.Min(scored.Count, maxResults);
        var entries = new EcdictEntry[count];
        for (var i = 0; i < count; i++)
        {
            entries[i] = _entries[scored[i].Entry];
        }

        return entries;
    }

    private static string NormalizeLatinText(string text)
    {
        var builder = new StringBuilder(text.Length);
        var pendingSpace = false;
        foreach (var character in text)
        {
            if (char.IsWhiteSpace(character))
            {
                pendingSpace = builder.Length > 0;
            }
            else if (char.IsLetterOrDigit(character) || character is '\'' or '-' or '\u2019')
            {
                if (pendingSpace)
                {
                    builder.Append(' ');
                    pendingSpace = false;
                }

                builder.Append(char.ToLowerInvariant(character is '\u2019' ? '\'' : character));
            }
            else
            {
                pendingSpace = builder.Length > 0;
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static int LowerBound(WordRef[] items, string value)
    {
        var low = 0;
        var high = items.Length;
        while (low < high)
        {
            var middle = low + ((high - low) / 2);
            if (string.CompareOrdinal(items[middle].Normalized, value) < 0)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        return low;
    }

    private readonly record struct WordRef(string Normalized, int Entry);

    private readonly record struct ScoredEntry(int Entry, int Score);
}
