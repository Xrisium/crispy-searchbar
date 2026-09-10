using System.Net;
using System.Text;

namespace CrispySearchbar.Core.Dictionary;

/// <summary>
/// StarDict 词条标记清洗：把 HTML / XDXF / Pango 等标记还原为纯文本。
/// 详情区与候选摘要都使用纯文本显示，因此这里统一剥离标签、还原实体并归整空白。
/// </summary>
public static class StarDictMarkup
{
    private static readonly HashSet<string> BlockTags = new(StringComparer.Ordinal)
    {
        "br", "p", "div", "li", "ul", "ol", "tr", "td", "th", "table",
        "blockquote", "section", "article", "header", "footer", "pre",
        "h1", "h2", "h3", "h4", "h5", "h6", "hr",
    };

    /// <summary>把一段带标记的词条文本转成多行纯文本（各行已去空白，空行被丢弃）。</summary>
    public static string ToPlainText(string? markup)
    {
        if (string.IsNullOrEmpty(markup))
        {
            return string.Empty;
        }

        var withoutEscapes = markup.Replace("\\n", "\n", StringComparison.Ordinal);
        var stripped = StripTags(withoutEscapes);
        var decoded = WebUtility.HtmlDecode(stripped);
        return string.Join('\n', SplitLines(decoded));
    }

    /// <summary>拆分纯文本为行：归整行内空白、去掉空行。</summary>
    public static IReadOnlyList<string> SplitLines(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var lines = new List<string>();
        foreach (var rawLine in text.Split(['\r', '\n'], StringSplitOptions.None))
        {
            var line = CollapseWhitespace(rawLine);
            if (line.Length > 0)
            {
                lines.Add(line);
            }
        }

        return lines;
    }

    private static string StripTags(string text)
    {
        var builder = new StringBuilder(text.Length);
        var position = 0;
        while (position < text.Length)
        {
            var character = text[position];
            if (character != '<')
            {
                builder.Append(character);
                position++;
                continue;
            }

            var end = text.IndexOf('>', position + 1);
            if (end < 0)
            {
                // 不完整的标签：保留剩余内容，避免丢失文本。
                builder.Append(' ');
                break;
            }

            if (IsBlockTag(text.AsSpan(position + 1, end - position - 1)))
            {
                builder.Append('\n');
            }

            position = end + 1;
        }

        return builder.ToString();
    }

    private static bool IsBlockTag(ReadOnlySpan<char> inner)
    {
        inner = inner.Trim();
        if (inner.Length > 0 && inner[0] == '/')
        {
            inner = inner[1..];
        }

        var end = 0;
        while (end < inner.Length
            && !char.IsWhiteSpace(inner[end])
            && inner[end] != '/')
        {
            end++;
        }

        if (end == 0)
        {
            return false;
        }

        return BlockTags.Contains(inner[..end].ToString().ToLowerInvariant());
    }

    private static string CollapseWhitespace(string text)
    {
        var builder = new StringBuilder(text.Length);
        var pendingSpace = false;
        foreach (var character in text)
        {
            if (char.IsWhiteSpace(character))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }
}
