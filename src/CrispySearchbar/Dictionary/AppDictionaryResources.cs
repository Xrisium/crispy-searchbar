using CrispySearchbar.Core.Dictionary;
using CrispySearchbar.Core.Localization;

namespace CrispySearchbar.Dictionary;

/// <summary>
/// 解析并加载内置/用户词典资源：
/// 汉英与英汉两个槽位支持同一组格式（.txt / .csv / .gz / .zip / StarDict .ifo），
/// 方向由槽位决定；优先级均为 settings.json 指定路径 &gt; 用户数据目录 &gt; 程序目录 &gt; 内嵌数据。
/// 内嵌数据（gzip 文本）随程序集分发，单文件发布时不产生任何外附数据文件；
/// 用户仍可在用户数据目录或程序目录放置同名文件覆盖内置数据。
/// </summary>
public sealed class AppDictionaryResources
{
    private const string CedictDisplayName = "CC-CEDICT";
    private const string EcdictDisplayName = "ECDICT";
    private const string StarDictDisplayName = "StarDict";
    private const string CedictDownloadUrl = "https://www.mdbg.net/chinese/dictionary?page=cedict";
    private const string EcdictDownloadUrl = "https://github.com/skywind3000/ECDICT";

    private static readonly DictionarySlot CedictSlot = new(
        CedictDisplayName,
        BundledDictionaryResources.CcCedict,
        CedictDownloadUrl);

    private static readonly DictionarySlot EcdictSlot = new(
        EcdictDisplayName,
        BundledDictionaryResources.Ecdict,
        EcdictDownloadUrl);

    private readonly AppStrings _strings;
    private readonly string? _cedictFilePath;
    private readonly string? _ecdictFilePath;
    private readonly string _userDataDirectory;
    private readonly string _programDirectory;

    public AppDictionaryResources(
        AppStrings strings,
        string? cedictFilePath,
        string? ecdictFilePath,
        string? userDataDirectory = null,
        string? programDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(strings);

        _strings = strings;
        _cedictFilePath = NormalizeConfiguredPath(cedictFilePath);
        _ecdictFilePath = NormalizeConfiguredPath(ecdictFilePath);
        _userDataDirectory = string.IsNullOrWhiteSpace(userDataDirectory)
            ? GetDefaultUserDataDirectory()
            : Path.GetFullPath(userDataDirectory);
        _programDirectory = string.IsNullOrWhiteSpace(programDirectory)
            ? AppContext.BaseDirectory
            : Path.GetFullPath(programDirectory);
    }

    /// <summary>用户数据目录中的汉英词典文件名（原始名或同名 .gz 都可覆盖内置数据）。</summary>
    public string CedictUserDataFilePath
        => Path.Combine(_userDataDirectory, BundledDictionaryResources.CcCedict.FileName);

    /// <summary>程序目录中的汉英词典文件名（可选覆盖层）。</summary>
    public string CedictProgramDataFilePath
        => Path.Combine(_programDirectory, BundledDictionaryResources.CcCedict.FileName);

    /// <summary>用户数据目录中的英汉词典文件名（原始名或同名 .gz 都可覆盖内置数据）。</summary>
    public string EcdictUserDataFilePath
        => Path.Combine(_userDataDirectory, BundledDictionaryResources.Ecdict.FileName);

    /// <summary>程序目录中的英汉词典文件名（可选覆盖层）。</summary>
    public string EcdictProgramDataFilePath
        => Path.Combine(_programDirectory, BundledDictionaryResources.Ecdict.FileName);

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
        var (source, message) = ResolveSource(
            CedictSlot,
            _cedictFilePath,
            _userDataDirectory,
            _programDirectory,
            _strings);
        if (source is not { } resolved)
        {
            return (null, message);
        }

        try
        {
            return (resolved.FilePath is { } path
                ? DictionarySourceLoader.LoadChineseIndex(path)
                : LoadEmbeddedChinese(resolved.Resource!), null);
        }
        catch (FileNotFoundException ex)
        {
            return (null, _strings.FormatDictionaryReadFailed(
                ResolveDisplayName(resolved, CedictSlot),
                ex.FileName));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or ArgumentException or InvalidDataException or NotSupportedException)
        {
            return (null, _strings.FormatDictionaryReadFailed(
                ResolveDisplayName(resolved, CedictSlot),
                ex.Message));
        }
    }

    private (EcdictIndex? Index, string? Message) LoadEcdict()
    {
        var (source, message) = ResolveSource(
            EcdictSlot,
            _ecdictFilePath,
            _userDataDirectory,
            _programDirectory,
            _strings);
        if (source is not { } resolved)
        {
            return (null, message);
        }

        try
        {
            return (resolved.FilePath is { } path
                ? DictionarySourceLoader.LoadEnglishIndex(path)
                : LoadEmbeddedEnglish(resolved.Resource!), null);
        }
        catch (FileNotFoundException ex)
        {
            return (null, _strings.FormatDictionaryReadFailed(
                ResolveDisplayName(resolved, EcdictSlot),
                ex.FileName));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or ArgumentException or InvalidDataException or NotSupportedException)
        {
            return (null, _strings.FormatDictionaryReadFailed(
                ResolveDisplayName(resolved, EcdictSlot),
                ex.Message));
        }
    }

    private static CedictIndex LoadEmbeddedChinese(BundledDictionaryResource resource)
    {
        using var stream = OpenEmbedded(resource);
        return DictionarySourceLoader.LoadChineseIndex(
            stream,
            resource.Format,
            resource.SourceName);
    }

    private static EcdictIndex LoadEmbeddedEnglish(BundledDictionaryResource resource)
    {
        using var stream = OpenEmbedded(resource);
        return DictionarySourceLoader.LoadEnglishIndex(
            stream,
            resource.Format,
            resource.SourceName);
    }

    private static Stream OpenEmbedded(BundledDictionaryResource resource)
        => resource.TryOpen()
           ?? throw new InvalidDataException($"内置词典数据缺失：{resource.ResourceName}");

    /// <summary>StarDict 词库用格式名做来源标识，避免与内置 CC-CEDICT/ECDICT 混淆。</summary>
    private static string ResolveDisplayName(DictionarySource source, DictionarySlot slot)
        => source.FilePath is { } path
            && DictionaryFileFormatDetector.Detect(path) == DictionaryFileFormat.StarDict
                ? StarDictDisplayName
                : slot.DisplayName;

    /// <summary>
    /// 解析槽位数据来源：配置路径 &gt; 用户数据目录 &gt; 程序目录（原始名优先，其次同名 .gz）&gt; 内嵌数据。
    /// 只有前三层都缺失且内嵌资源不存在时才返回提示文案。
    /// </summary>
    private static (DictionarySource? Source, string? Message) ResolveSource(
        DictionarySlot slot,
        string? configuredPath,
        string userDataDirectory,
        string programDirectory,
        AppStrings strings)
    {
        ArgumentNullException.ThrowIfNull(strings);

        if (configuredPath is not null)
        {
            var full = Path.GetFullPath(configuredPath);
            return File.Exists(full)
                ? (DictionarySource.FromFile(full), null)
                : (null, strings.FormatConfiguredDictionaryFileMissing(
                    slot.DisplayName,
                    configuredPath));
        }

        foreach (var candidate in slot.Resource.ExternalFileNames)
        {
            var userDataFile = Path.Combine(userDataDirectory, candidate);
            if (File.Exists(userDataFile))
            {
                return (DictionarySource.FromFile(userDataFile), null);
            }
        }

        foreach (var candidate in slot.Resource.ExternalFileNames)
        {
            var programFile = Path.Combine(programDirectory, candidate);
            if (File.Exists(programFile))
            {
                return (DictionarySource.FromFile(programFile), null);
            }
        }

        return slot.Resource.Exists
            ? (DictionarySource.FromResource(slot.Resource), null)
            : (null, strings.FormatDictionaryDataFileNotFound(
                slot.DisplayName,
                slot.Resource.FileName,
                Path.Combine(userDataDirectory, slot.Resource.FileName),
                Path.Combine(programDirectory, slot.Resource.FileName),
                slot.DownloadUrl));
    }

    private static string? NormalizeConfiguredPath(string? path)
        => string.IsNullOrWhiteSpace(path) ? null : Path.GetFullPath(path);

    private static string GetDefaultUserDataDirectory() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CrispySearchbar");

    /// <summary>一个词典槽位的固定元数据：显示名、外部覆盖文件名与内嵌资源。</summary>
    private sealed record DictionarySlot(
        string DisplayName,
        BundledDictionaryResource Resource,
        string DownloadUrl);

    /// <summary>已解析的数据来源：外部文件路径与内嵌资源名互斥。</summary>
    private readonly record struct DictionarySource(
        string? FilePath,
        BundledDictionaryResource? Resource)
    {
        public static DictionarySource FromFile(string path) => new(path, null);

        public static DictionarySource FromResource(BundledDictionaryResource resource)
            => new(null, resource);
    }
}
