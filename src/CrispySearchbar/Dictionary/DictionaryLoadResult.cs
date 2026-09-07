using CrispySearchbar.Core.Dictionary;

namespace CrispySearchbar.Dictionary;

/// <summary>词典数据加载结果：成功时包含索引；失败时只给用户可读提示。</summary>
public sealed record DictionaryLoadResult(
    DictionaryIndex? Index,
    string? DataFilePath,
    string? Message)
{
    public bool IsReady => Index is not null;

    public static DictionaryLoadResult Missing(string message)
        => new(Index: null, DataFilePath: null, Message: message);

    public static DictionaryLoadResult Failed(string? dataFilePath, string message)
        => new(Index: null, DataFilePath: dataFilePath, Message: message);
}
