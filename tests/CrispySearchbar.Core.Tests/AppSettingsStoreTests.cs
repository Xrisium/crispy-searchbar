using CrispySearchbar.Core.Configuration;
using Xunit;

namespace CrispySearchbar.Core.Tests;

public class AppSettingsStoreTests
{
    [Fact]
    public void DefaultDirectory_IsApplicationBaseDirectory()
    {
        Assert.Equal(AppContext.BaseDirectory, AppSettingsStore.GetDefaultDirectoryPath());
    }

    [Fact]
    public void LoadOrDefault_CreatesDefaultFileOnFirstRun()
    {
        var dir = CreateTempDirectory();
        try
        {
            var settings = AppSettingsStore.LoadOrDefault(dir);

            Assert.Equal(ThemePreference.System, settings.Theme);
            Assert.Equal(SearchEngineKind.Baidu, settings.SearchEngine);
            Assert.Contains("{0}", settings.AskAiUrlTemplate);
            Assert.True(settings.ClearQueryOnHide);
            Assert.True(File.Exists(Path.Combine(dir, AppSettingsStore.FileName)));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void SaveAndLoad_RoundTripsValues()
    {
        var dir = CreateTempDirectory();
        try
        {
            var expected = new AppSettings
            {
                Theme = ThemePreference.Dark,
                SearchEngine = SearchEngineKind.Google,
                AskAiUrlTemplate = "https://chat.deepseek.com/?q={0}",
                ClearQueryOnHide = false,
            };

            AppSettingsStore.Save(expected, dir);
            var loaded = AppSettingsStore.LoadOrDefault(dir);

            Assert.Equal(ThemePreference.Dark, loaded.Theme);
            Assert.Equal(SearchEngineKind.Google, loaded.SearchEngine);
            Assert.Equal(expected.AskAiUrlTemplate, loaded.AskAiUrlTemplate);
            Assert.False(loaded.ClearQueryOnHide);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void LoadOrDefault_FallsBackToDefaultsWhenFileIsCorrupt()
    {
        var dir = CreateTempDirectory();
        try
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, AppSettingsStore.FileName), "{ not valid json");

            var settings = AppSettingsStore.LoadOrDefault(dir);

            Assert.Equal(ThemePreference.System, settings.Theme);
            Assert.Equal(SearchEngineKind.Baidu, settings.SearchEngine);
            Assert.Contains("{0}", settings.AskAiUrlTemplate);
            Assert.True(settings.ClearQueryOnHide);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private static string CreateTempDirectory()
        => Path.Combine(Path.GetTempPath(), "crispy-searchbar-tests-" + Guid.NewGuid().ToString("N"));
}


