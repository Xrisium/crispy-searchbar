using CrispySearchbar.Core.Dictionary;

namespace CrispySearchbar.Dictionary;

/// <summary>
/// 解析并加载 CC-CEDICT 词典文件。优先级：settings.json 指定路径 &gt; 用户数据目录 &gt; 程序目录。
/// 用户数据目录中的文件不会被发布物的内置文件覆盖。
/// </summary>
public sealed class AppDictionaryDataSource
{
    public const string DataFileName = "cedict_ts.u8";
    public const string DownloadUrl = "https://www.mdbg.net/chinese/dictionary?page=cedict";

    private readonly string? _configuredFilePath;

    public AppDictionaryDataSource(string? configuredFilePath)
    {
        _configuredFilePath = string.IsNullOrWhiteSpace(configuredFilePath)
            ? null
            : Path.GetFullPath(configuredFilePath);
    }

    public string UserDataFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CrispySearchbar",
        DataFileName);

    public string BundledDataFilePath => Path.Combine(AppContext.BaseDirectory, DataFileName);

    /// <summary>按优先级返回实际应使用的数据文件；不存在时返回 null。</summary>
    public string? ResolveDataFilePath()
    {
        if (_configuredFilePath is not null)
        {
            return File.Exists(_configuredFilePath) ? _configuredFilePath : null;
        }

        if (File.Exists(UserDataFilePath))
        {
            return UserDataFilePath;
        }

        return File.Exists(BundledDataFilePath) ? BundledDataFilePath : null;
    }

    public async Task<DictionaryLoadResult> LoadAsync(CancellationToken cancellationToken)
    {
        var path = ResolveDataFilePath();
        if (path is null)
        {
            return DictionaryLoadResult.Missing(BuildMissingMessage());
        }

        try
        {
            return await Task.Run(
                () => new DictionaryLoadResult(
                    CedictIndexLoader.LoadFile(path),
                    path,
                    Message: null),
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return DictionaryLoadResult.Failed(path, $"词典数据读取失败：{ex.Message}");
        }
    }

    private string BuildMissingMessage()
    {
        if (_configuredFilePath is not null)
        {
            return $"在 settings.json 中指定的词典文件不存在：{_configuredFilePath}";
        }

        return "未找到 CC-CEDICT 词典数据文件（cedict_ts.u8）。"
            + $"可将文件放到：{UserDataFilePath} 或 {BundledDataFilePath}。"
            + $"下载地址：{DownloadUrl}";
    }
}
