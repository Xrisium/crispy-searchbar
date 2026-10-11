using System.IO.Compression;
using System.Text.Json;

namespace CrispySearchbar.Core.Emoji;

public static class EmojiResources
{
    public static Stream OpenImages() => Open("twemoji.zip");

    public static EmojiIndex LoadIndex()
    {
        using var compressed = Open("catalog.json.gz");
        using var stream = new GZipStream(compressed, CompressionMode.Decompress);
        var entries = JsonSerializer.Deserialize<EmojiEntry[]>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("Missing Emoji catalog.");
        return new EmojiIndex(entries);
    }

    private static Stream Open(string file) => typeof(EmojiResources).Assembly
        .GetManifestResourceStream("CrispySearchbar.Core.Data.Emoji." + file)
        ?? throw new InvalidDataException("Missing embedded Emoji resource: " + file);
}
