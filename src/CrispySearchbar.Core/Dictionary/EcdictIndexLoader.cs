using System.Text;

namespace CrispySearchbar.Core.Dictionary;

/// <summary>从磁盘加载 ECDICT CSV 并构建英→汉索引。应在后台任务中调用。</summary>
public static class EcdictIndexLoader
{
    public static EcdictIndex LoadFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"ECDICT 词典文件不存在：{filePath}", filePath);
        }

        using var reader = new StreamReader(
            filePath,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true);
        return EcdictIndex.Build(EcdictParser.Parse(reader));
    }
}
