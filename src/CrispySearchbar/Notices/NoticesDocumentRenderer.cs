using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using CrispySearchbar.Core.Notices;

namespace CrispySearchbar.Notices;

/// <summary>
/// 把内嵌声明文档渲染成 Avalonia 控件树。
/// 颜色与字号全部来自窗口 XAML 里的样式类，这里只负责结构与文本。
/// </summary>
internal static class NoticesDocumentRenderer
{
    private const string TextClass = "noticesText";
    private const string PlainTextClass = "noticesPlainText";

    /// <summary>渲染一份文档；链接点击由 <paramref name="onLinkActivated"/> 处理。</summary>
    public static Control Build(NoticeDocument document, Action<string> onLinkActivated)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(onLinkActivated);

        if (document.Format == NoticeDocumentFormat.PlainText)
        {
            return new SelectableTextBlock
            {
                Text = document.Text,
                Classes = { PlainTextClass },
            };
        }

        var host = new StackPanel { Spacing = 8 };
        foreach (var block in NoticesMarkdownParser.Parse(document.Text))
        {
            host.Children.Add(BuildBlock(block, onLinkActivated));
        }

        return host;
    }

    private static Control BuildBlock(NoticesBlock block, Action<string> onLinkActivated)
        => block switch
        {
            NoticesHeading heading => CreateTextBlock(
                heading.Inlines,
                HeadingClass(heading.Level),
                onLinkActivated),
            NoticesParagraph paragraph => CreateTextBlock(
                paragraph.Inlines,
                TextClass,
                onLinkActivated),
            NoticesBullet bullet => CreateBullet(bullet, onLinkActivated),
            NoticesTable table => CreateTable(table, onLinkActivated),
            _ => new StackPanel(),
        };

    private static string HeadingClass(int level) => level switch
    {
        <= 1 => "noticesHeading1",
        2 => "noticesHeading2",
        _ => "noticesHeading3",
    };

    private static Control CreateBullet(NoticesBullet bullet, Action<string> onLinkActivated)
    {
        var marker = new TextBlock
        {
            Text = "•",
            Classes = { TextClass },
            Margin = new Thickness(2, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Top,
        };
        var content = CreateTextBlock(bullet.Inlines, TextClass, onLinkActivated);

        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("14,*") };
        row.Children.Add(marker);
        Grid.SetColumn(content, 1);
        row.Children.Add(content);
        return row;
    }

    private static Control CreateTable(NoticesTable table, Action<string> onLinkActivated)
    {
        var columnCount = table.Rows.Count == 0
            ? 0
            : table.Rows.Max(row => row.Count);
        if (columnCount == 0)
        {
            return new StackPanel();
        }

        var grid = new Grid();
        for (var column = 0; column < columnCount; column++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        }

        foreach (var _ in table.Rows)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        }

        for (var rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
        {
            var row = table.Rows[rowIndex];
            for (var column = 0; column < row.Count; column++)
            {
                var cell = CreateTextBlock(
                    row[column],
                    rowIndex == 0 ? "noticesTableHeader" : "noticesTableCell",
                    onLinkActivated);
                cell.Margin = new Thickness(0, 2, 12, rowIndex == 0 ? 6 : 2);
                Grid.SetRow(cell, rowIndex);
                Grid.SetColumn(cell, column);
                grid.Children.Add(cell);
            }
        }

        return grid;
    }

    private static TextBlock CreateTextBlock(
        IReadOnlyList<NoticesInline> inlines,
        string styleClass,
        Action<string> onLinkActivated)
    {
        var textBlock = new TextBlock { Classes = { styleClass } };
        var linkRanges = new List<(int Start, int Length, string Url)>();
        var position = 0;

        foreach (var inline in inlines)
        {
            var run = new Run(inline.Text);
            if (inline.IsBold)
            {
                run.FontWeight = FontWeight.SemiBold;
            }

            if (inline.LinkUrl is { } url)
            {
                run.TextDecorations = TextDecorations.Underline;
                linkRanges.Add((position, inline.Text.Length, url));
            }

            textBlock.Inlines!.Add(run);
            position += inline.Text.Length;
        }

        if (linkRanges.Count > 0)
        {
            AttachLinkHandler(textBlock, linkRanges, onLinkActivated);
        }

        return textBlock;
    }

    /// <summary>
    /// 行内链接不是独立控件：按下时用文本命中测试把点击位置换算成字符下标，
    /// 再映射回对应的链接 URL。
    /// </summary>
    private static void AttachLinkHandler(
        TextBlock textBlock,
        IReadOnlyList<(int Start, int Length, string Url)> linkRanges,
        Action<string> onLinkActivated)
    {
        textBlock.Cursor = new Cursor(StandardCursorType.Hand);
        textBlock.PointerPressed += (_, args) =>
        {
            var hit = textBlock.TextLayout.HitTestPoint(args.GetPosition(textBlock));
            if (!hit.IsInside)
            {
                return;
            }

            foreach (var (start, length, url) in linkRanges)
            {
                if (hit.TextPosition >= start && hit.TextPosition < start + length)
                {
                    onLinkActivated(url);
                    args.Handled = true;
                    return;
                }
            }
        };
    }
}
