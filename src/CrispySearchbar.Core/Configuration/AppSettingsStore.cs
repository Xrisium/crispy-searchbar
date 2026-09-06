using System.Text.Json;
using System.Text.Json.Serialization;

namespace CrispySearchbar.Core.Configuration;

/// <summary>读取和保存 JSON 配置文件；损坏或不可读时回退到默认值。</summary>
public static class AppSettingsStore
{
    public const string FileName = "settings.json";
    public const string DefaultDirectoryName = "CrispySearchbar";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public static string GetDefaultDirectoryPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return string.IsNullOrWhiteSpace(appData)
            ? Path.Combine(Directory.GetCurrentDirectory(), DefaultDirectoryName)
            : Path.Combine(appData, DefaultDirectoryName);
    }

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
