using System.Text.Json;
using CrispySearchbar.Core.Configuration;
using CrispySearchbar.Core.Localization;
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

            Assert.Equal(AppLanguage.System, settings.Language);
            Assert.Equal(ThemePreference.System, settings.Theme);
            Assert.Equal(SearchEngineKind.Baidu, settings.SearchEngine);
            Assert.Contains("{0}", settings.AskAiUrlTemplate);
            Assert.True(settings.ClearQueryOnHide);
            Assert.Null(settings.DictionaryFilePath);
            Assert.Null(settings.EcdictFilePath);
            Assert.True(File.Exists(Path.Combine(dir, AppSettingsStore.FileName)));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GeneratedFile_ListsEveryConfigurableItem()
    {
        var dir = CreateTempDirectory();
        try
        {
            AppSettingsStore.LoadOrDefault(dir);
            var json = File.ReadAllText(Path.Combine(dir, AppSettingsStore.FileName));
            using var document = JsonDocument.Parse(json);

            var names = document.RootElement.EnumerateObject().Select(p => p.Name).ToArray();
            Assert.Equal(7, names.Length);
            Assert.Contains("language", names);
            Assert.Contains("theme", names);
            Assert.Contains("searchEngine", names);
            Assert.Contains("clearQueryOnHide", names);
            Assert.Contains("askAiUrlTemplate", names);
            Assert.Contains("dictionaryFilePath", names);
            Assert.Contains("ecdictFilePath", names);
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
                Language = AppLanguage.English,
                Theme = ThemePreference.Dark,
                SearchEngine = SearchEngineKind.Google,
                AskAiUrlTemplate = "https://chat.deepseek.com/?q={0}",
                ClearQueryOnHide = false,
                DictionaryFilePath = "C:\\dict\\cedict_ts.u8",
                EcdictFilePath = "C:\\dict\\ecdict.csv",
            };

            AppSettingsStore.Save(expected, dir);
            var loaded = AppSettingsStore.LoadOrDefault(dir);

            Assert.Equal(AppLanguage.English, loaded.Language);
            Assert.Equal(ThemePreference.Dark, loaded.Theme);
            Assert.Equal(SearchEngineKind.Google, loaded.SearchEngine);
            Assert.Equal(expected.AskAiUrlTemplate, loaded.AskAiUrlTemplate);
            Assert.False(loaded.ClearQueryOnHide);
            Assert.Equal("C:\\dict\\cedict_ts.u8", loaded.DictionaryFilePath);
            Assert.Equal("C:\\dict\\ecdict.csv", loaded.EcdictFilePath);
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

            Assert.Equal(AppLanguage.System, settings.Language);
            Assert.Equal(ThemePreference.System, settings.Theme);
            Assert.Equal(SearchEngineKind.Baidu, settings.SearchEngine);
            Assert.Contains("{0}", settings.AskAiUrlTemplate);
            Assert.True(settings.ClearQueryOnHide);
            Assert.Null(settings.DictionaryFilePath);
            Assert.Null(settings.EcdictFilePath);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private static string CreateTempDirectory()
        => Path.Combine(Path.GetTempPath(), "crispy-searchbar-tests-" + Guid.NewGuid().ToString("N"));
}
