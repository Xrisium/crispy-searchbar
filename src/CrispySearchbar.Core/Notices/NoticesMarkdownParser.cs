using System.Text;

namespace CrispySearchbar.Core.Notices;

/// <summary>
/// <c>THIRD_PARTY_NOTICES.md</c> 用到的 Markdown 子集解析器：
/// 标题、段落、无序列表、管道表格，以及行内的粗体、行内代码与链接。
/// 不追求完整的 Markdown 兼容，只保证声明文档渲染整洁。
/// </summary>
public static class NoticesMarkdownParser
{
    private const int MaxHeadingLevel = 6;

    /// <summary>把 Markdown 文本拆成块序列。</summary>
    public static IReadOnlyList<NoticesBlock> Parse(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        var blocks = new List<NoticesBlock>();
        var lines = markdown
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');
        var paragraph = new List<string>();

        for (var index = 0; index < lines.Length; index++)
        {
            var trimmed = lines[index].Trim();
            if (trimmed.Length == 0)
            {
                FlushParagraph(blocks, paragraph);
                continue;
            }

            if (TryParseHeading(trimmed) is { } heading)
            {
                FlushParagraph(blocks, paragraph);
                blocks.Add(heading);
                continue;
            }

            if (IsTableStart(lines, index))
            {
                FlushParagraph(blocks, paragraph);
                blocks.Add(ParseTable(lines, ref index));
                continue;
            }

            if (TryParseBullet(trimmed) is { } bullet)
            {
                FlushParagraph(blocks, paragraph);
                blocks.Add(new NoticesBullet(ParseInlines(bullet)));
                continue;
            }

            paragraph.Add(trimmed);
        }

        FlushParagraph(blocks, paragraph);
        return blocks;
    }

    /// <summary>解析行内片段：粗体、行内代码与链接；其余按纯文本。</summary>
    public static IReadOnlyList<NoticesInline> ParseInlines(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var inlines = new List<NoticesInline>();
        var plain = new StringBuilder();

        for (var index = 0; index < text.Length;)
        {
            if (TryReadLink(text, index, out var link, out var linkLength))
            {
                FlushInline(inlines, plain);
                inlines.Add(link);
                index += linkLength;
                continue;
            }

            if (TryReadBold(text, index, out var bold, out var boldLength))
            {
                FlushInline(inlines, plain);
                inlines.Add(bold);
                index += boldLength;
                continue;
            }

            if (text[index] == '`')
            {
                var end = text.IndexOf('`', index + 1);
                if (end > index)
                {
                    plain.Append(text, index + 1, end - index - 1);
                    index = end + 1;
                    continue;
                }
            }

            plain.Append(text[index]);
            index++;
        }

        FlushInline(inlines, plain);
        return inlines;
    }

    private static void FlushParagraph(List<NoticesBlock> blocks, List<string> paragraph)
    {
        if (paragraph.Count == 0)
        {
            return;
        }

        blocks.Add(new NoticesParagraph(ParseInlines(string.Join(' ', paragraph))));
        paragraph.Clear();
    }

    private static void FlushInline(List<NoticesInline> inlines, StringBuilder plain)
    {
        if (plain.Length == 0)
        {
            return;
        }

        inlines.Add(new NoticesInline(plain.ToString()));
        plain.Clear();
    }

    private static NoticesHeading? TryParseHeading(string trimmed)
    {
        var level = 0;
        while (level < trimmed.Length
            && level < MaxHeadingLevel
            && trimmed[level] == '#')
        {
            level++;
        }

        if (level == 0 || level >= trimmed.Length || trimmed[level] != ' ')
        {
            return null;
        }

        return new NoticesHeading(level, ParseInlines(trimmed[(level + 1)..].Trim()));
    }

    private static string? TryParseBullet(string trimmed)
    {
        if (trimmed.Length < 2 || trimmed[1] != ' ')
        {
            return null;
        }

        return trimmed[0] is '-' or '*' or '+'
            ? trimmed[2..].Trim()
            : null;
    }

    private static bool IsTableStart(string[] lines, int index)
        => lines[index].TrimStart().StartsWith('|')
           && index + 1 < lines.Length
           && IsTableSeparator(lines[index + 1]);

    private static bool IsTableSeparator(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.Length == 0 || !trimmed.StartsWith('|'))
        {
            return false;
        }

        var hasDash = false;
        foreach (var character in trimmed)
        {
            switch (character)
            {
                case '-':
                    hasDash = true;
                    break;
                case '|':
                case ':':
                case ' ':
                    break;
                default:
                    return false;
            }
        }

        return hasDash;
    }

    private static NoticesTable ParseTable(string[] lines, ref int index)
    {
        var rows = new List<IReadOnlyList<IReadOnlyList<NoticesInline>>>
        {
            SplitCells(lines[index]),
        };

        // 跳过表头与分隔行，接着收集连续的数据行。
        index += 2;
        while (index < lines.Length && lines[index].TrimStart().StartsWith('|'))
        {
            rows.Add(SplitCells(lines[index]));
            index++;
        }

        // 外层 for 会再自增一次，这里回退一格避免吃掉下一行。
        index--;
        return new NoticesTable(rows);
    }

    private static IReadOnlyList<IReadOnlyList<NoticesInline>> SplitCells(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.StartsWith('|'))
        {
            trimmed = trimmed[1..];
        }

        if (trimmed.EndsWith('|'))
        {
            trimmed = trimmed[..^1];
        }

        return trimmed
            .Split('|')
            .Select(cell => ParseInlines(cell.Trim()))
            .ToArray();
    }

    private static bool TryReadLink(
        string text,
        int start,
        out NoticesInline link,
        out int length)
    {
        link = default!;
        length = 0;
        if (text[start] != '[')
        {
            return false;
        }

        var labelEnd = text.IndexOf(']', start + 1);
        if (labelEnd < 0 || labelEnd + 1 >= text.Length || text[labelEnd + 1] != '(')
        {
            return false;
        }

        var urlEnd = text.IndexOf(')', labelEnd + 2);
        if (urlEnd < 0)
        {
            return false;
        }

        var label = text[(start + 1)..labelEnd];
        var url = text[(labelEnd + 2)..urlEnd].Trim();
        link = new NoticesInline(label, IsBold: false, LinkUrl: url);
        length = urlEnd - start + 1;
        return true;
    }

    private static bool TryReadBold(
        string text,
        int start,
        out NoticesInline bold,
        out int length)
    {
        bold = default!;
        length = 0;
        if (start + 1 >= text.Length || text[start] != '*' || text[start + 1] != '*')
        {
            return false;
        }

        var end = text.IndexOf("**", start + 2, StringComparison.Ordinal);
        if (end < 0)
        {
            return false;
        }

        bold = new NoticesInline(text[(start + 2)..end], IsBold: true);
        length = end - start + 2;
        return true;
    }
}
