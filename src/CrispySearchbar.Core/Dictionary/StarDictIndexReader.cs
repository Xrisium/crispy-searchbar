using System.Buffers.Binary;
using System.Text;

namespace CrispySearchbar.Core.Dictionary;

/// <summary>
/// StarDict .idx / .syn 二进制读取器。
/// .idx 记录：<c>词头\0</c> + 偏移（4 字节，idxoffsetbits=64 时 8 字节，大端）+ 长度（4 字节大端）。
/// .syn 记录：<c>词头\0</c> + 目标 .idx 记录序号（4 字节大端）。
/// </summary>
public static class StarDictIndexReader
{
    /// <summary>一条 .idx 记录：词头与正文在 .dict 中的偏移/长度。</summary>
    public readonly record struct IndexEntry(string Word, long Offset, int Size);

    public static IReadOnlyList<IndexEntry> ReadIndex(byte[] bytes, int offsetBits)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        var offsetByteCount = offsetBits == 64 ? 8 : 4;
        var entries = new List<IndexEntry>();
        var position = 0;
        while (position < bytes.Length)
        {
            var terminator = Array.IndexOf(bytes, (byte)0, position);
            if (terminator < 0)
            {
                break;
            }

            var word = Encoding.UTF8.GetString(bytes, position, terminator - position);
            position = terminator + 1;
            if (position + offsetByteCount + 4 > bytes.Length)
            {
                break;
            }

            var offset = offsetByteCount == 8
                ? (long)BinaryPrimitives.ReadUInt64BigEndian(bytes.AsSpan(position, 8))
                : BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(position, 4));
            position += offsetByteCount;
            var size = (int)BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(position, 4));
            position += 4;

            if (word.Length == 0)
            {
                continue;
            }

            entries.Add(new IndexEntry(word, offset, size));
        }

        return entries;
    }

    /// <summary>读取同义词表：词头 + 目标 .idx 记录序号。</summary>
    public static IReadOnlyList<(string Word, int Index)> ReadSynonyms(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        var synonyms = new List<(string Word, int Index)>();
        var position = 0;
        while (position < bytes.Length)
        {
            var terminator = Array.IndexOf(bytes, (byte)0, position);
            if (terminator < 0)
            {
                break;
            }

            var word = Encoding.UTF8.GetString(bytes, position, terminator - position);
            position = terminator + 1;
            if (position + 4 > bytes.Length)
            {
                break;
            }

            var target = (int)BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(position, 4));
            position += 4;

            if (word.Length == 0)
            {
                continue;
            }

            synonyms.Add((word, target));
        }

        return synonyms;
    }
}
