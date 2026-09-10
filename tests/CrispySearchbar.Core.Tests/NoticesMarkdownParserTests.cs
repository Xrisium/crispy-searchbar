using CrispySearchbar.Core.Notices;
using Xunit;

namespace CrispySearchbar.Core.Tests;

public class NoticesMarkdownParserTests
{
    [Fact]
    public void Parse_ReadsHeadingsParagraphsAndBullets()
    {
        const string markdown = """
            # 标题

            第一行
            第二行

            - 项目一
            - 项目二
            """;

        var blocks = NoticesMarkdownParser.Parse(markdown);

        var heading = Assert.IsType<NoticesHeading>(blocks[0]);
        Assert.Equal(1, heading.Level);
        Assert.Equal("标题", TextOf(heading.Inlines));

        var paragraph = Assert.IsType<NoticesParagraph>(blocks[1]);
        Assert.Equal("第一行 第二行", TextOf(paragraph.Inlines));
        Assert.Equal(2, blocks.OfType<NoticesBullet>().Count());
    }

    [Fact]
    public void Parse_ReadsPipeTables()
    {
        const string markdown = """
            | 数据 | 文件 | 许可证 |
            |---|---|---|
            | CC-CEDICT | `data/cc-cedict/cedict.txt.gz` | [CC BY-SA 4.0](licenses/CC-BY-SA-4.0.txt) |
            | ECDICT | `data/ecdict/ecdict.csv.gz` | [MIT](licenses/MIT.txt) |
            """;

        var table = Assert.IsType<NoticesTable>(
            Assert.Single(NoticesMarkdownParser.Parse(markdown)));

        Assert.Equal(3, table.Rows.Count);
        Assert.Equal(3, table.Rows[0].Count);
        Assert.Equal("数据", TextOf(table.Rows[0][0]));
        Assert.Equal("CC-CEDICT", TextOf(table.Rows[1][0]));

        // 行内代码只保留内容，去掉反引号。
        Assert.Equal("data/ecdict/ecdict.csv.gz", TextOf(table.Rows[2][1]));

        // 链接保留显示文本与目标。
        var link = Assert.Single(table.Rows[2][2]);
        Assert.Equal("MIT", link.Text);
        Assert.Equal("licenses/MIT.txt", link.LinkUrl);
    }

    [Fact]
    public void ParseInlines_MarksBoldAndKeepsInlineCode()
    {
        var inlines = NoticesMarkdownParser.ParseInlines("**重要** 与 `path/to/file` 文本");

        Assert.Equal("重要", inlines[0].Text);
        Assert.True(inlines[0].IsBold);
        // 行内代码去掉反引号后并入相邻纯文本片段。
        Assert.Contains(inlines, inline => inline.Text.Contains("path/to/file", StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_WithoutTrailingNewline_KeepsLastParagraph()
    {
        var blocks = NoticesMarkdownParser.Parse("只有一段");

        var paragraph = Assert.IsType<NoticesParagraph>(Assert.Single(blocks));
        Assert.Equal("只有一段", TextOf(paragraph.Inlines));
    }

    private static string TextOf(IReadOnlyList<NoticesInline> inlines)
        => string.Concat(inlines.Select(inline => inline.Text));
}
