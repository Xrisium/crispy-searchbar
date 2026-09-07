namespace CrispySearchbar.Core.Dictionary;

/// <summary>从磁盘加载 CC-CEDICT 文本并构建索引。应在后台任务中调用，避免阻塞 UI。</summary>
public static class CedictIndexLoader
{
    public static DictionaryIndex LoadFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"CC-CEDICT 词典文件不存在：{filePath}", filePath);
        }

        var entries = CedictParser.ParseLines(File.ReadLines(filePath));
        return DictionaryIndex.Build(entries);
    }
}
