using System.Text.Json;
using System.Text.Json.Serialization;

namespace CrispySearchbar.Core.Configuration;

/// <summary>读取和保存 JSON 配置文件；损坏或不可读时回退到默认值。
/// 配置文件默认放在程序（发布目录）同目录，方便用户直接编辑；
/// 程序目录不可写（例如放进 Program Files 或只读介质）时回退到用户本地数据目录，
/// 保证便携版仍能保存设置。图形化设置界面读写的就是实际生效的那一份文件。</summary>
public static class AppSettingsStore
{
    public const string FileName = "settings.json";

    private static string? _effectiveDirectory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>默认目录：程序所在目录（开发时是 bin 输出目录，发布后是 exe 同目录）。</summary>
    public static string GetDefaultDirectoryPath()
        => AppContext.BaseDirectory;

    /// <summary>程序目录不可写时的回退目录：%LOCALAPPDATA%\CrispySearchbar。</summary>
    public static string GetFallbackDirectoryPath()
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CrispySearchbar");

    /// <summary>
    /// 实际生效的配置文件目录：优先程序目录，不可写时使用回退目录。
    /// 结果在进程内只解析一次，避免每次读配置都做写盘探测。
    /// </summary>
    public static string GetEffectiveDirectoryPath()
        => _effectiveDirectory ??= ResolveEffectiveDirectory(
            IsDirectoryWritable,
            GetDefaultDirectoryPath(),
            GetFallbackDirectoryPath());

    /// <summary>实际生效的配置文件路径（设置面板展示与打开的就是它）。</summary>
    public static string GetEffectiveFilePath()
        => Path.Combine(GetEffectiveDirectoryPath(), FileName);

    public static string GetSettingsFilePath(string? directoryPath = null)
        => Path.Combine(directoryPath ?? GetEffectiveDirectoryPath(), FileName);

    public static AppSettings LoadOrDefault(string? directoryPath = null)
    {
        var path = GetSettingsFilePath(directoryPath);
        try
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var existing = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (existing is not null)
                {
                    var shouldRewrite = false;
                    var normalizedModes = ModePreferenceNormalizer.Normalize(
                        existing.ModePreferences);
                    if (!ModePreferenceNormalizer.IsEquivalent(
                            existing.ModePreferences,
                            normalizedModes))
                    {
                        existing.ModePreferences = normalizedModes;
                        shouldRewrite = true;
                    }

                    if (ShortcutNormalizer.Normalize(existing))
                    {
                        shouldRewrite = true;
                    }

                    if (shouldRewrite)
                    {
                        Save(existing, directoryPath);
                    }

                    return existing;
                }
            }

            var defaults = new AppSettings();
            Save(defaults, directoryPath);
            return defaults;
        }
        catch (Exception ex) when (
            ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings, string? directoryPath = null)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var path = GetSettingsFilePath(directoryPath);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, JsonSerializer.Serialize(settings, JsonOptions));
    }

    /// <summary>
    /// 目录可写性探测：能落地一份临时文件即视为可写（探测文件立即删除）。
    /// 只做一次判断，失败一律按不可写处理，避免把异常抛给启动流程。
    /// </summary>
    internal static bool IsDirectoryWritable(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            return false;
        }

        try
        {
            Directory.CreateDirectory(directory);
            var probePath = Path.Combine(directory, $".{FileName}.probe");
            File.WriteAllText(probePath, string.Empty);
            File.Delete(probePath);
            return true;
        }
        catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or NotSupportedException
            or ArgumentException
            or PathTooLongException)
        {
            return false;
        }
    }

    /// <summary>按“程序目录优先、不可写则回退”的规则选出生效目录。</summary>
    internal static string ResolveEffectiveDirectory(
        Func<string, bool> isWritable,
        string defaultDirectory,
        string fallbackDirectory)
    {
        ArgumentNullException.ThrowIfNull(isWritable);
        return isWritable(defaultDirectory) ? defaultDirectory : fallbackDirectory;
    }
}

