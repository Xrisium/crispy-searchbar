using CrispySearchbar.Core.Configuration;
using Xunit;

namespace CrispySearchbar.Core.Tests;

public class AppSettingsDirectoryTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "crispy-migration-" + Guid.NewGuid().ToString("N"));
    private string Destination => Path.Combine(_root, "roaming");
    private string Legacy(string name) => Path.Combine(_root, name, "settings.json");
    private void Write(string path, string json) { Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, json); }

    [Fact]
    public void DefaultDirectory_LivesInRoamingAppData()
    {
        Assert.Equal(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CrispySearchbar"), AppSettingsStore.GetDefaultDirectoryPath());
        Assert.Equal(AppSettingsStore.GetDefaultDirectoryPath(), AppSettingsStore.GetEffectiveDirectoryPath());
    }
    [Fact]
    public void MigratesProgramSettingsAndArchivesBothLegacyFiles()
    {
        var first = Legacy("program"); var second = Legacy("local");
        Write(first, "{\"theme\":\"dark\",\"executeShortcut\":\"Ctrl+J\"}");
        Write(second, "{\"theme\":\"light\"}");
        var settings = AppSettingsStore.LoadAndMigrate(Destination, [first, second]);
        Assert.Equal(ThemePreference.Dark, settings.Theme);
        Assert.Equal("Ctrl+J", settings.ExecuteShortcut);
        Assert.False(File.Exists(first)); Assert.False(File.Exists(second));
        Assert.Equal(2, Directory.GetFiles(Path.Combine(Destination, "backups")).Length);
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(first)!));
        Assert.Equal(ThemePreference.Dark, AppSettingsStore.LoadAndMigrate(Destination, [first, second]).Theme);
    }
    [Fact]
    public void ExistingNewSettingsWinAndLegacyIsArchived()
    {
        AppSettingsStore.Save(new AppSettings { Theme = ThemePreference.Light }, Destination);
        var old = Legacy("program"); Write(old, "{\"theme\":\"dark\"}");
        Assert.Equal(ThemePreference.Light, AppSettingsStore.LoadAndMigrate(Destination, [old]).Theme);
        Assert.False(File.Exists(old));
        Assert.Single(Directory.GetFiles(Path.Combine(Destination, "backups")));
    }
    [Fact]
    public void CorruptSourcesAreArchivedBeforeTryingNextLocation()
    {
        Write(Path.Combine(Destination, "settings.json"), "invalid new");
        var first = Legacy("program"); var second = Legacy("local");
        Write(first, "invalid old"); Write(second, "{\"theme\":\"dark\"}");
        Assert.Equal(ThemePreference.Dark, AppSettingsStore.LoadAndMigrate(Destination, [first, second]).Theme);
        Assert.Equal(3, Directory.GetFiles(Path.Combine(Destination, "backups")).Length);
    }
    [Fact]
    public void ArchiveFailurePreservesOriginal()
    {
        Directory.CreateDirectory(Destination); File.WriteAllText(Path.Combine(Destination, "backups"), "block directory");
        var source = Legacy("program"); Write(source, "{\"theme\":\"dark\"}");
        IReadOnlyList<string> warnings = [];
        Assert.Equal(ThemePreference.Dark, AppSettingsStore.LoadAndMigrate(Destination, [source], w => warnings = w).Theme);
        Assert.True(File.Exists(source)); Assert.NotEmpty(warnings);
    }
    [Fact]
    public void WriteFailurePreservesLegacyAndDoesNotFallbackToProgramDirectory()
    {
        Directory.CreateDirectory(_root); File.WriteAllText(Destination, "block directory");
        var source = Legacy("program"); Write(source, "{\"theme\":\"dark\"}");
        IReadOnlyList<string> warnings = [];
        Assert.Equal(ThemePreference.Dark, AppSettingsStore.LoadAndMigrate(Destination, [source], w => warnings = w).Theme);
        Assert.True(File.Exists(source)); Assert.Single(Directory.GetFiles(Path.GetDirectoryName(source)!));
        Assert.NotEmpty(warnings);
    }
    [Fact]
    public void ReadOnlyLegacyIsBackedUpAndPreservedWhenRemovalIsDenied()
    {
        var source = Legacy("program"); Write(source, "{\"theme\":\"dark\"}");
        var original = File.ReadAllBytes(source);
        File.SetAttributes(source, FileAttributes.ReadOnly);
        try
        {
            IReadOnlyList<string> warnings = [];
            Assert.Equal(ThemePreference.Dark, AppSettingsStore.LoadAndMigrate(Destination, [source], w => warnings = w).Theme);
            Assert.Equal(original, File.ReadAllBytes(Assert.Single(Directory.GetFiles(Path.Combine(Destination, "backups")))));
            Assert.True(File.Exists(Path.Combine(Destination, "settings.json")));
            if (OperatingSystem.IsWindows()) { Assert.True(File.Exists(source)); Assert.NotEmpty(warnings); }
        }
        finally { if (File.Exists(source)) File.SetAttributes(source, FileAttributes.Normal); }
    }
    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
}
