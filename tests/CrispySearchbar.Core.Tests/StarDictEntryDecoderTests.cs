using System.Text;
using CrispySearchbar.Core.Dictionary;
using Xunit;

namespace CrispySearchbar.Core.Tests;

public class StarDictEntryDecoderTests
{
    [Fact]
    public void Decode_Sequenced_TrimsTrailingNullOnLastField()
    {
        using var stream = new MemoryStream();
        stream.Write(Encoding.UTF8.GetBytes("n. 苹果"));
        stream.WriteByte(0);

        var fields = StarDictEntryDecoder.Decode(stream.ToArray(), "m");

        var field = Assert.Single(fields);
        Assert.Equal("n. 苹果", field.Text);
    }

    [Fact]
    public void Decode_TypeTagged_SkipsBinaryFields()
    {
        using var stream = new MemoryStream();
        stream.WriteByte((byte)'W');
        stream.Write([0, 0, 0, 7]);
        stream.WriteByte((byte)'m');
        stream.Write(Encoding.UTF8.GetBytes("n. 苹果"));
        stream.WriteByte(0);

        var fields = StarDictEntryDecoder.Decode(stream.ToArray(), sametypeSequence: null);

        var field = Assert.Single(fields);
        Assert.Equal('m', field.Type);
        Assert.Equal("n. 苹果", field.Text);
    }

    [Fact]
    public void Decode_TypeTagged_StopsAtUnknownType()
    {
        using var stream = new MemoryStream();
        stream.WriteByte((byte)'m');
        stream.Write(Encoding.UTF8.GetBytes("apple"));
        stream.WriteByte(0);
        stream.WriteByte((byte)'Z');
        stream.Write([1, 2, 3, 4]);

        var fields = StarDictEntryDecoder.Decode(stream.ToArray(), sametypeSequence: null);

        var field = Assert.Single(fields);
        Assert.Equal("apple", field.Text);
    }

    [Fact]
    public void Decode_Sequenced_ReadsFieldsInOrder()
    {
        var payload = StarDictFixture.SequencedPayload("tmg", "/ˈæpl/", "n. 苹果", "<b>apple</b>");

        var fields = StarDictEntryDecoder.Decode(payload, "tmg");

        Assert.Collection(
            fields,
            field => Assert.Equal(('t', "/ˈæpl/"), (field.Type, field.Text)),
            field => Assert.Equal(('m', "n. 苹果"), (field.Type, field.Text)),
            field => Assert.Equal(('g', "<b>apple</b>"), (field.Type, field.Text)));
    }
}
