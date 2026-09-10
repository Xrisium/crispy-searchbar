using CrispySearchbar.Core.Dictionary;
using Xunit;

namespace CrispySearchbar.Core.Tests;

public class StarDictMarkupTests
{
    [Fact]
    public void ToPlainText_StripsTagsAndDecodesEntities()
    {
        var text = StarDictMarkup.ToPlainText("<b>apple</b><br/>a fruit &amp; more");

        Assert.Equal("apple\na fruit & more", text);
    }

    [Fact]
    public void ToPlainText_CollapsesWhitespaceAndDropsBlankLines()
    {
        var text = StarDictMarkup.ToPlainText("  first   line \n\n\tsecond line  ");

        Assert.Equal("first line\nsecond line", text);
    }

    [Fact]
    public void ToPlainText_ConvertsLiteralEscapedNewlines()
    {
        var text = StarDictMarkup.ToPlainText("n. 苹果\\na fruit");

        Assert.Equal("n. 苹果\na fruit", text);
    }
}
