using System.Buffers.Binary;
using System.Text;

namespace CrispySearchbar.Core.Dictionary;

/// <summary>StarDict 词条中的一个字段：类型字符与原始文本（已按 UTF-8 解码，未做标记清洗）。</summary>
public readonly record struct StarDictField(char Type, string Text);

/// <summary>
/// StarDict 词条正文解码器。
/// 存在 sametypesequence 时按序列顺序切分字段（文本字段以 \0 结尾，最后一个字段可省略结尾）；
/// 否则每个字段前带 1 字节类型标记。文本类类型之外的字段按其长度安全跳过。
/// </summary>
public static class StarDictEntryDecoder
{
    private static readonly HashSet<char> TextTypes = ['m', 'l', 'g', 'x', 'h', 'k', 'w'];

    public static bool IsTextType(char type) => TextTypes.Contains(type);

    public static IReadOnlyList<StarDictField> Decode(ReadOnlySpan<byte> payload, string? sametypeSequence)
    {
        return string.IsNullOrEmpty(sametypeSequence)
            ? DecodeTypeTagged(payload)
            : DecodeSequenced(payload, sametypeSequence);
    }

    private static IReadOnlyList<StarDictField> DecodeSequenced(
        ReadOnlySpan<byte> payload,
        string sametypeSequence)
    {
        var fields = new List<StarDictField>(sametypeSequence.Length);
        var position = 0;
        for (var i = 0; i < sametypeSequence.Length && position <= payload.Length; i++)
        {
            var type = sametypeSequence[i];
            var isLast = i == sametypeSequence.Length - 1;
            if (IsTextType(type) || type is 't' or 'y')
            {
                if (isLast)
                {
                    fields.Add(new StarDictField(type, DecodeText(payload[position..])));
                    position = payload.Length;
                }
                else
                {
                    if (!TryReadNullTerminated(payload, ref position, out var text))
                    {
                        break;
                    }

                    fields.Add(new StarDictField(type, text));
                }

                continue;
            }

            if (!TrySkipField(payload, ref position, type))
            {
                break;
            }
        }

        return fields;
    }

    private static IReadOnlyList<StarDictField> DecodeTypeTagged(ReadOnlySpan<byte> payload)
    {
        var fields = new List<StarDictField>();
        var position = 0;
        while (position < payload.Length)
        {
            var type = (char)payload[position];
            position++;
            if (IsTextType(type) || type is 't' or 'y')
            {
                if (!TryReadNullTerminated(payload, ref position, out var text))
                {
                    fields.Add(new StarDictField(type, DecodeText(payload[position..])));
                    break;
                }

                fields.Add(new StarDictField(type, text));
                continue;
            }

            if (!TrySkipField(payload, ref position, type))
            {
                break;
            }
        }

        return fields;
    }

    /// <summary>按类型跳过非文本字段：W/P 为 4 字节整数，r 为 4 字节长度前缀 + 数据，其余视为未知并停止。</summary>
    private static bool TrySkipField(ReadOnlySpan<byte> payload, ref int position, char type)
    {
        switch (type)
        {
            case 'W':
            case 'P':
                if (position + 4 > payload.Length)
                {
                    return false;
                }

                position += 4;
                return true;
            case 'r':
                if (position + 4 > payload.Length)
                {
                    return false;
                }

                var length = (int)BinaryPrimitives.ReadUInt32BigEndian(payload.Slice(position, 4));
                position += 4;
                if (length < 0 || position + length > payload.Length)
                {
                    return false;
                }

                position += length;
                return true;
            default:
                return false;
        }
    }

    private static bool TryReadNullTerminated(
        ReadOnlySpan<byte> payload,
        ref int position,
        out string text)
    {
        var remaining = payload[position..];
        var terminator = remaining.IndexOf((byte)0);
        if (terminator < 0)
        {
            text = string.Empty;
            return false;
        }

        text = DecodeText(remaining[..terminator]);
        position += terminator + 1;
        return true;
    }

    private static string DecodeText(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length == 0)
        {
            return string.Empty;
        }

        // 最后一个字段按规范可省略结尾 \0，但部分词库仍会写入，这里统一去掉尾部的 \0。
        return Encoding.UTF8.GetString(bytes).TrimEnd('\0');
    }
}
