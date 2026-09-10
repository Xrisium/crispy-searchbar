using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace CrispySearchbar.Core.Tests;

/// <summary>
/// 生成临时 StarDict 词库夹具：按代码写出 .ifo/.idx/.dict(.dz)/.syn，便于测试解析与加载。
/// </summary>
internal sealed class StarDictFixture : IDisposable
{
    private readonly string _directory;
    private readonly string _basePath;

    private StarDictFixture(string directory, string baseName)
    {
        _directory = directory;
        _basePath = Path.Combine(directory, baseName);
    }

    public string IfoPath => _basePath + ".ifo";

    public string IdxPath => _basePath + ".idx";

    public string DictPath => _basePath + ".dict";

    public string DictDzPath => _basePath + ".dict.dz";

    public static StarDictFixture Create(string baseName = "testdict")
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "crispy-searchbar-stardict-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return new StarDictFixture(directory, baseName);
    }

    public void WriteIfo(string body)
        => File.WriteAllText(IfoPath, body, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

    public void WriteEntries(
        IReadOnlyList<(string Word, byte[] Payload)> entries,
        bool compressed = false,
        int offsetBits = 32,
        IReadOnlyList<(string Word, int Target)>? synonyms = null)
    {
        using var index = new MemoryStream();
        using var data = new MemoryStream();
        foreach (var (word, payload) in entries)
        {
            WriteNullTerminated(index, word);
            if (offsetBits == 64)
            {
                WriteUInt64(index, (ulong)data.Length);
            }
            else
            {
                WriteUInt32(index, (uint)data.Length);
            }

            WriteUInt32(index, (uint)payload.Length);
            data.Write(payload);
        }

        File.WriteAllBytes(IdxPath, index.ToArray());
        var dataBytes = data.ToArray();
        if (compressed)
        {
            using var file = File.Create(DictDzPath);
            using var gzip = new GZipStream(file, CompressionLevel.Optimal);
            gzip.Write(dataBytes);
        }
        else
        {
            File.WriteAllBytes(DictPath, dataBytes);
        }

        if (synonyms is not null)
        {
            using var syn = new MemoryStream();
            foreach (var (word, target) in synonyms)
            {
                WriteNullTerminated(syn, word);
                WriteUInt32(syn, (uint)target);
            }

            File.WriteAllBytes(_basePath + ".syn", syn.ToArray());
        }
    }

    /// <summary>类型标记 + 文本 + \0，逐字段拼接（无 sametypesequence 的词条格式）。</summary>
    public static byte[] TaggedPayload(params (char Type, string Text)[] fields)
    {
        using var stream = new MemoryStream();
        foreach (var (type, text) in fields)
        {
            stream.WriteByte((byte)type);
            stream.Write(Encoding.UTF8.GetBytes(text));
            stream.WriteByte(0);
        }

        return stream.ToArray();
    }

    /// <summary>按 sametypesequence 顺序拼接文本字段，非末尾字段以 \0 结尾。</summary>
    public static byte[] SequencedPayload(string sequence, params string[] texts)
    {
        if (sequence.Length != texts.Length)
        {
            throw new ArgumentException("字段类型数与文本数必须一致。", nameof(texts));
        }

        using var stream = new MemoryStream();
        for (var i = 0; i < texts.Length; i++)
        {
            stream.Write(Encoding.UTF8.GetBytes(texts[i]));
            if (i < texts.Length - 1)
            {
                stream.WriteByte(0);
            }
        }

        return stream.ToArray();
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
            // 临时目录清理失败不影响测试结果。
        }
    }

    private static void WriteNullTerminated(Stream stream, string word)
    {
        stream.Write(Encoding.UTF8.GetBytes(word));
        stream.WriteByte(0);
    }

    private static void WriteUInt32(Stream stream, uint value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(buffer, value);
        stream.Write(buffer);
    }

    private static void WriteUInt64(Stream stream, ulong value)
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(buffer, value);
        stream.Write(buffer);
    }
}
