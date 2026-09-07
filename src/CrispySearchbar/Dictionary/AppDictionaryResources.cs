using CrispySearchbar.Core.Dictionary;

namespace CrispySearchbar.Dictionary;

/// <summary>
/// 解析并加载内置/用户词典资源：
/// CC-CEDICT（汉→英）与 ECDICT（英→汉）各自独立加载，
/// 优先级均为 settings.json 指定路径 &gt; 用户数据目录 &gt; 程序目录。
/// </summary>
public sealed class AppDictionaryResources
{
    public const string CedictFileName = "cedict_ts.u8";
    public const string EcdictFileName = "ecdict.csv";

    private const string CedictDownloadUrl = "https://www.mdbg.net/chinese/dictionary?page=cedict";
    private const string EcdictDownloadUrl = "https://github.com/skywind3000/ECDICT";

    private readonly string? _cedictFilePath;
    private readonly string? _ecdictFilePath;

    public AppDictionaryResources(string? cedictFilePath, string? ecdictFilePath)
    {
        _cedictFilePath = NormalizeConfiguredPath(cedictFilePath);
        _ecdictFilePath = NormalizeConfiguredPath(ecdictFilePath);
    }

    public string CedictUserDataFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CrispySearchbar",
        CedictFileName);

    public string CedictBundledDataFilePath => Path.Combine(AppContext.BaseDirectory, CedictFileName);

    public string EcdictUserDataFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CrispySearchbar",
        EcdictFileName);

    public string EcdictBundledDataFilePath => Path.Combine(AppContext.BaseDirectory, EcdictFileName);

    /// <summary>并行加载两个方向的词典；单个数据源缺失/失败不影响另一个。</summary>
    public async Task<DictionaryLoadResult> LoadAsync(CancellationToken cancellationToken)
    {
        var cedictTask = Task.Run(() => LoadCedict(), cancellationToken);
        var ecdictTask = Task.Run(() => LoadEcdict(), cancellationToken);
        await Task.WhenAll(cedictTask, ecdictTask).ConfigureAwait(false);

        return new DictionaryLoadResult(
            cedictTask.Result.Index,
            cedictTask.Result.Message,
            ecdictTask.Result.Index,
            ecdictTask.Result.Message);
    }

    private (CedictIndex? Index, string? Message) LoadCedict()
    {
        var (path, message) = ResolveDataFile(
            _cedictFilePath,
            CedictUserDataFilePath,
            CedictBundledDataFilePath,
            CedictFileName,
            "CC-CEDICT",
            CedictDownloadUrl);
        if (path is null)
        {
            return (null, message);
        }

        try
        {
            return (CedictIndexLoader.LoadFile(path), null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return (null, $"CC-CEDICT 词典数据读取失败：{ex.Message}");
        }
    }

    private (EcdictIndex? Index, string? Message) LoadEcdict()
    {
        var (path, message) = ResolveDataFile(
            _ecdictFilePath,
            EcdictUserDataFilePath,
            EcdictBundledDataFilePath,
            EcdictFileName,
            "ECDICT",
            EcdictDownloadUrl);
        if (path is null)
        {
            return (null, message);
        }

        try
        {
            return (EcdictIndexLoader.LoadFile(path), null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return (null, $"ECDICT 词典数据读取失败：{ex.Message}");
        }
    }

    private static (string? Path, string? Message) ResolveDataFile(
        string? configuredPath,
        string userDataFilePath,
        string bundledFilePath,
        string fileName,
        string displayName,
        string downloadUrl)
    {
        if (configuredPath is not null)
        {
            var full = Path.GetFullPath(configuredPath);
            return File.Exists(full)
                ? (full, null)
                : (null, $"在 settings.json 中指定的 {displayName} 词典文件不存在：{configuredPath}");
        }

        if (File.Exists(userDataFilePath))
        {
            return (userDataFilePath, null);
        }

        if (File.Exists(bundledFilePath))
        {
            return (bundledFilePath, null);
        }

        return (null, $"未找到 {displayName} 词典数据文件（{fileName}）。"
            + $"可将文件放到：{userDataFilePath} 或 {bundledFilePath}。"
            + $"下载地址：{downloadUrl}");
    }

    private static string? NormalizeConfiguredPath(string? path)
        => string.IsNullOrWhiteSpace(path) ? null : Path.GetFullPath(path);
}
