using CrispySearchbar.Core.Notices;
using Xunit;

namespace CrispySearchbar.Core.Tests;

public class NoticesDocumentsTests
{
    [Fact]
    public void All_ContainsThirdPartyNoticesAndEveryLicenseText()
    {
        var documents = NoticesDocuments.All;

        Assert.True(documents.Count >= 6, $"应至少有 6 份内嵌文档，实际 {documents.Count} 份。");
        Assert.Equal(NoticesDocuments.ThirdPartyNoticesId, documents[0].Id);
        Assert.True(documents[0].IsThirdPartyNotices);
        Assert.Equal(NoticeDocumentFormat.Markdown, documents[0].Format);
        Assert.Contains("CC-CEDICT", documents[0].Text, StringComparison.Ordinal);

        foreach (var id in new[] { "Apache-2.0", "BSD-3-Clause", "CC-BY-SA-4.0", "ISC", "MIT" })
        {
            var document = NoticesDocuments.Find(id);
            Assert.NotNull(document);
            Assert.Equal(NoticeDocumentFormat.PlainText, document!.Format);
            Assert.False(string.IsNullOrWhiteSpace(document.Text));
        }
    }

    [Fact]
    public void ResolveLink_MapsRelativeLicenseLinksToEmbeddedDocuments()
    {
        Assert.Equal("MIT", NoticesDocuments.ResolveLink("licenses/MIT.txt")!.Id);
        Assert.Equal(
            "CC-BY-SA-4.0",
            NoticesDocuments.ResolveLink("licenses/CC-BY-SA-4.0.txt")!.Id);

        // 外部链接不是内嵌文档，应交给浏览器处理。
        Assert.Null(NoticesDocuments.ResolveLink(
            "https://github.com/Xrisium/crispy-searchbar"));
    }

    [Fact]
    public void ThirdPartyNotices_RendersHeadingsAndTables()
    {
        var blocks = NoticesMarkdownParser.Parse(NoticesDocuments.All[0].Text);

        Assert.Contains(blocks, block => block is NoticesHeading);
        Assert.Contains(blocks, block => block is NoticesTable);
        Assert.Contains(blocks, block => block is NoticesBullet);
    }
}
