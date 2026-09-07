using System.Text;

namespace CrispySearchbar.Core.Dictionary;

/// <summary>
/// CC-CEDICT（汉→英）不可变内存索引：简体/繁体词头前缀索引。
/// 仅处理中文查询；英文查询应走 ECDICT（英→汉）索引。
/// </summary>
public sealed class CedictIndex
{
    private const int MaxRawCandidatesPerQuery = 600;

    private readonly DictionaryEntry[] _entries;
    private readonly HeadwordRef[] _headwords;

    private CedictIndex(DictionaryEntry[] entries, HeadwordRef[] headwords)
    {
        _entries = entries;
        _headwords = headwords;
    }

    public int Count => _entries.Length;

    public IReadOnlyList<DictionaryEntry> Entries => _entries;

    public static CedictIndex Empty { get; } = new([], []);

    public static CedictIndex Build(IEnumerable<DictionaryEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var entryList = entries
            .Where(entry => entry is not null
                && !string.IsNullOrWhiteSpace(entry.Simplified)
                && !string.IsNullOrWhiteSpace(entry.Traditional))
            .ToArray();
        if (entryList.Length == 0)
        {
            return Empty;
        }

        var headwords = new List<HeadwordRef>(entryList.Length * 2);
        for (var entryIndex = 0; entryIndex < entryList.Length; entryIndex++)
        {
            var entry = entryList[entryIndex];
            headwords.Add(new HeadwordRef(entry.Simplified, entryIndex));
            if (!string.Equals(entry.Traditional, entry.Simplified, StringComparison.Ordinal))
            {
                headwords.Add(new HeadwordRef(entry.Traditional, entryIndex));
            }
        }

        headwords.Sort(static (left, right) =>
        {
            var byText = string.CompareOrdinal(left.Text, right.Text);
            return byText != 0 ? byText : left.Entry.CompareTo(right.Entry);
        });

        return new CedictIndex(entryList, headwords.ToArray());
    }

    /// <summary>中文查询：简体/繁体词头精确、前缀与包含匹配。</summary>
    public IReadOnlyList<DictionaryEntry> Search(string? rawQuery, int maxResults = 12)
    {
        if (string.IsNullOrWhiteSpace(rawQuery) || maxResults <= 0)
        {
            return [];
        }

        var query = rawQuery.Trim();
        if (query.Length == 0)
        {
            return [];
        }

        var exact = new List<int>();
        var prefix = new List<int>();
        var seen = new HashSet<int>();

        var lower = LowerBound(_headwords, query);
        for (var i = lower; i < _headwords.Length; i++)
        {
            var headword = _headwords[i];
            if (!headword.Text.StartsWith(query, StringComparison.Ordinal))
            {
                break;
            }

            if (!seen.Add(headword.Entry))
            {
                continue;
            }

            if (string.Equals(headword.Text, query, StringComparison.Ordinal))
            {
                exact.Add(headword.Entry);
            }
            else
            {
                prefix.Add(headword.Entry);
            }

            if (exact.Count >= maxResults)
            {
                break;
            }
        }

        var results = new List<ScoredEntry>(exact.Count + prefix.Count);
        AddScored(results, exact, score: 0);
        if (results.Count < maxResults)
        {
            AddScored(results, prefix, score: 1);
        }

        if (results.Count < maxResults)
        {
            for (var entryIndex = 0; entryIndex < _entries.Length && results.Count < maxResults; entryIndex++)
            {
                if (seen.Contains(entryIndex))
                {
                    continue;
                }

                var entry = _entries[entryIndex];
                if (entry.Simplified.Contains(query, StringComparison.Ordinal)
                    || entry.Traditional.Contains(query, StringComparison.Ordinal))
                {
                    results.Add(new ScoredEntry(entryIndex, 2));
                    seen.Add(entryIndex);
                }
            }
        }

        return Resolve(results, maxResults);
    }

    private static void AddScored(List<ScoredEntry> target, IReadOnlyList<int> entryIndexes, int score)
    {
        foreach (var entryIndex in entryIndexes)
        {
            target.Add(new ScoredEntry(entryIndex, score));
        }
    }

    private IReadOnlyList<DictionaryEntry> Resolve(List<ScoredEntry> scored, int maxResults)
    {
        scored.Sort(static (left, right) =>
        {
            var byScore = left.Score.CompareTo(right.Score);
            return byScore != 0 ? byScore : left.Entry.CompareTo(right.Entry);
        });

        var count = Math.Min(scored.Count, maxResults);
        var entries = new DictionaryEntry[count];
        for (var i = 0; i < count; i++)
        {
            entries[i] = _entries[scored[i].Entry];
        }

        return entries;
    }

    private static int LowerBound(HeadwordRef[] items, string value)
    {
        var low = 0;
        var high = items.Length;
        while (low < high)
        {
            var middle = low + ((high - low) / 2);
            if (string.CompareOrdinal(items[middle].Text, value) < 0)
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

    private readonly record struct HeadwordRef(string Text, int Entry);

    private readonly record struct ScoredEntry(int Entry, int Score);
}
