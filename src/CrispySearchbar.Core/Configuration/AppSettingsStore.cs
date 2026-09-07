using System.Text.Json;
using System.Text.Json.Serialization;

namespace CrispySearchbar.Core.Configuration;

/// <summary>读取和保存 JSON 配置文件；损坏或不可读时回退到默认值。
/// 配置文件放在程序（发布目录）同目录，方便用户直接编辑，也为后续图形化设置界面保留同一数据模型。</summary>
public static class AppSettingsStore
{
    public const string FileName = "settings.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>默认目录：程序所在目录（开发时是 bin 输出目录，发布后是 exe 同目录）。</summary>
    public static string GetDefaultDirectoryPath()
        => AppContext.BaseDirectory;

    public static string GetSettingsFilePath(string? directoryPath = null)
        => Path.Combine(directoryPath ?? GetDefaultDirectoryPath(), FileName);

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
}

