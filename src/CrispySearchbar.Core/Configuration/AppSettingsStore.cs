using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CrispySearchbar.Core.Configuration;

/// <summary>User settings live in roaming AppData. Archive legacy files before removal.</summary>
public static class AppSettingsStore
{
    public const string FileName = "settings.json";
    public static IReadOnlyList<string> LoadWarnings { get; private set; } = [];
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };
    public static string GetDefaultDirectoryPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CrispySearchbar");
    public static string GetEffectiveDirectoryPath() => GetDefaultDirectoryPath();
    public static string GetEffectiveFilePath() => GetSettingsFilePath();
    public static string GetSettingsFilePath(string? directoryPath = null)
        => Path.Combine(directoryPath ?? GetEffectiveDirectoryPath(), FileName);

    public static AppSettings LoadOrDefault(string? directoryPath = null)
        => LoadAndMigrate(directoryPath ?? GetDefaultDirectoryPath(), directoryPath is null
            ? [Path.Combine(AppContext.BaseDirectory, FileName), Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CrispySearchbar", FileName)]
            : []);

    internal static AppSettings LoadAndMigrate(string directory, IReadOnlyList<string> legacyPaths,
        Action<IReadOnlyList<string>>? reportWarnings = null)
    {
        var warnings = new List<string>();
        var path = GetSettingsFilePath(directory);
        AppSettings? settings = null;
        var verifiedDestination = false;
        var needsSave = false;
        try
        {
            if (File.Exists(path))
            {
                var bytes = File.ReadAllBytes(path);
                settings = Parse(bytes);
                verifiedDestination = settings is not null;
                if (settings is null)
                    Archive(directory, path, bytes);
            }
            foreach (var legacyPath in legacyPaths.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (Path.GetFullPath(legacyPath).Equals(Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase)
                    || !File.Exists(legacyPath))
                    continue;
                try
                {
                    var bytes = File.ReadAllBytes(legacyPath);
                    var imported = Parse(bytes);
                    if (settings is null && imported is not null)
                    {
                        // Keep readable legacy preferences in memory even when roaming AppData
                        // cannot be written. Only a verified destination permits source removal.
                        settings = imported;
                        Save(imported, directory);
                        settings = Parse(File.ReadAllBytes(path))
                            ?? throw new IOException("Unable to verify migrated settings.");
                        verifiedDestination = true;
                    }
                    if (verifiedDestination || imported is null)
                        Archive(directory, legacyPath, bytes);
                }
                catch (Exception ex) when (IsFileError(ex)) { warnings.Add($"{legacyPath}: {ex.Message}"); }
            }
            if (settings is null)
            {
                settings = new AppSettings();
                needsSave = true;
            }
            var modes = ModePreferenceNormalizer.Normalize(settings.ModePreferences);
            if (!ModePreferenceNormalizer.IsEquivalent(settings.ModePreferences, modes))
            {
                settings.ModePreferences = modes;
                needsSave = true;
            }
            needsSave |= ShortcutNormalizer.Normalize(settings);
            if (!Enum.IsDefined(settings.EmojiDefaultSkinTone))
            {
                settings.EmojiDefaultSkinTone = EmojiSkinTone.Neutral;
                needsSave = true;
            }
            if (needsSave)
                Save(settings, directory);
        }
        catch (Exception ex) when (IsFileError(ex)) { warnings.Add($"{path}: {ex.Message}"); }
        LoadWarnings = warnings;
        reportWarnings?.Invoke(warnings);
        return settings ?? new AppSettings();
    }

    private static AppSettings? Parse(byte[] bytes)
    {
        try
        {
            return JsonSerializer.Deserialize<AppSettings>(bytes, JsonOptions);
        }
        catch (JsonException) { return null; }
    }

    private static void Archive(string directory, string source, byte[] bytes)
    {
        var backupDirectory = Path.Combine(directory, "backups");
        Directory.CreateDirectory(backupDirectory);
        var identity = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(source))))[..12];
        var hash = Convert.ToHexString(SHA256.HashData(bytes));
        var backup = Path.Combine(backupDirectory, $"settings-{identity}-{hash}.json");
        if (!File.Exists(backup))
            WriteAtomic(backup, bytes);
        if (!File.ReadAllBytes(backup).AsSpan().SequenceEqual(bytes))
            throw new IOException("Unable to verify archived settings.");
        // Preserve a file edited by another instance while migration was in progress.
        if (!File.ReadAllBytes(source).AsSpan().SequenceEqual(bytes))
            throw new IOException("Settings changed during migration; the source file was preserved.");
        File.Delete(source);
    }

    public static void Save(AppSettings settings, string? directoryPath = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        WriteAtomic(GetSettingsFilePath(directoryPath), JsonSerializer.SerializeToUtf8Bytes(settings, JsonOptions));
    }

    internal static void WriteAtomic(string path, byte[] bytes)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporary, path, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    private static bool IsFileError(Exception ex)
        => ex is IOException or UnauthorizedAccessException or JsonException;
}
