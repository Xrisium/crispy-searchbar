using System.IO.Compression;
using Avalonia.Media.Imaging;
using CrispySearchbar.Core.Emoji;

namespace CrispySearchbar.Emoji;

/// <summary>Bounded cache. Candidate objects retain their own bitmap references after eviction.</summary>
public sealed class EmojiImageCache : IDisposable
{
    private readonly ZipArchive _archive = new(EmojiResources.OpenImages(), ZipArchiveMode.Read);
    private readonly Dictionary<string, Bitmap> _images = new();
    private readonly Queue<string> _order = new();
    public Bitmap Get(string name)
    {
        if (_images.TryGetValue(name, out var bitmap))
            return bitmap;
        using var stream = (_archive.GetEntry(name + ".png") ?? throw new InvalidDataException(name)).Open();
        // ZIP entry streams do not support seeking; native image decoders may need to rewind.
        using var png = new MemoryStream();
        stream.CopyTo(png);
        png.Position = 0;
        bitmap = new Bitmap(png);
        _images[name] = bitmap;
        _order.Enqueue(name);
        // Evicted images are reclaimed when neither the UI nor cache retains them.
        if (_images.Count > 96)
            _images.Remove(_order.Dequeue());
        return bitmap;
    }
    public void Dispose()
    {
        _archive.Dispose();
        _images.Clear();
        _order.Clear();
    }
}
