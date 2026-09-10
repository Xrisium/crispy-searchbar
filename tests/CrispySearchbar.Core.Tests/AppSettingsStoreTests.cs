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
            Assert.True(settings.HideOnEscape);
            Assert.Equal(
                ModePreferenceDefaults.BuiltInOrder,
                settings.ModePreferences.Select(preference => preference.Key));
            Assert.All(settings.ModePreferences, preference => Assert.True(preference.Enabled));
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
            Assert.Equal(15, names.Length);
            Assert.Contains("language", names);
            Assert.Contains("theme", names);
            Assert.Contains("searchBarOffset", names);
            Assert.Contains("searchEngine", names);
            Assert.Contains("clearQueryOnHide", names);
            Assert.Contains("askAiUrlTemplate", names);
            Assert.Contains("modePreferences", names);
            Assert.Contains("dictionaryFilePath", names);
            Assert.Contains("ecdictFilePath", names);
            Assert.Contains("toggleVisibilityShortcut", names);
            Assert.Contains("cycleModeShortcut", names);
            Assert.Contains("hideOnEscape", names);
            Assert.Contains("executeShortcut", names);
            Assert.Contains("selectPreviousShortcut", names);
            Assert.Contains("selectNextShortcut", names);
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
                HideOnEscape = false,
                ModePreferences =
                [
                    new ModePreference(ModePreferenceDefaults.WebSearch, enabled: false),
                    new ModePreference(ModePreferenceDefaults.Wikipedia, enabled: true),
                    new ModePreference(ModePreferenceDefaults.AskAi, enabled: true),
                    new ModePreference(ModePreferenceDefaults.Dictionary, enabled: false),
                ],
                DictionaryFilePath = "C:\\dict\\cedict_1_0_ts_utf-8_mdbg.txt",
                EcdictFilePath = "C:\\dict\\ecdict.csv",
                SearchBarOffset = new ScreenOffset(160, -90),
            };

            AppSettingsStore.Save(expected, dir);
            var loaded = AppSettingsStore.LoadOrDefault(dir);

            Assert.Equal(AppLanguage.English, loaded.Language);
            Assert.Equal(ThemePreference.Dark, loaded.Theme);
            Assert.Equal(SearchEngineKind.Google, loaded.SearchEngine);
            Assert.Equal(expected.AskAiUrlTemplate, loaded.AskAiUrlTemplate);
            Assert.False(loaded.ClearQueryOnHide);
            Assert.False(loaded.HideOnEscape);
            Assert.Equal(
                expected.ModePreferences.Select(preference => new
                {
                    preference.Key,
                    preference.Enabled,
                }),
                loaded.ModePreferences.Select(preference => new
                {
                    preference.Key,
                    preference.Enabled,
                }));
            Assert.Equal("C:\\dict\\cedict_1_0_ts_utf-8_mdbg.txt", loaded.DictionaryFilePath);
            Assert.Equal("C:\\dict\\ecdict.csv", loaded.EcdictFilePath);
            Assert.Equal(new ScreenOffset(160, -90), loaded.SearchBarOffset);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void LoadOrDefault_WithoutOffsetKey_UsesCenteredDefault()
    {
        var dir = CreateTempDirectory();
        try
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(
                Path.Combine(dir, AppSettingsStore.FileName),
                """
                {
                  "theme": "dark"
                }
                """);

            var settings = AppSettingsStore.LoadOrDefault(dir);

            Assert.Equal(ThemePreference.Dark, settings.Theme);
            Assert.Equal(ScreenOffset.Default, settings.SearchBarOffset);
            Assert.True(settings.SearchBarOffset.IsDefault);
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
            Assert.Equal(4, settings.ModePreferences.Length);
            Assert.Null(settings.DictionaryFilePath);
            Assert.Null(settings.EcdictFilePath);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void LoadOrDefault_NormalizesInvalidShortcutsAndRewritesFile()
    {
        var dir = CreateTempDirectory();
        try
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(
                Path.Combine(dir, AppSettingsStore.FileName),
                """
                {
                  "toggleVisibilityShortcut": "Not A Hotkey",
                  "executeShortcut": "Ctrl+Q"
                }
                """);

            var settings = AppSettingsStore.LoadOrDefault(dir);

            Assert.Equal("Alt+Space", settings.ToggleVisibilityShortcut);
            Assert.True(settings.HideOnEscape);
            var json = File.ReadAllText(Path.Combine(dir, AppSettingsStore.FileName));
            using var document = JsonDocument.Parse(json);
            Assert.Equal(
                "Alt+Space",
                document.RootElement.GetProperty("toggleVisibilityShortcut").GetString());
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void LoadOrDefault_PreservesEmptyShortcutsAndIgnoresLegacyHideShortcut()
    {
        var dir = CreateTempDirectory();
        try
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(
                Path.Combine(dir, AppSettingsStore.FileName),
                """
                {
                  "toggleVisibilityShortcut": "",
                  "hideShortcut": "Ctrl+Q",
                  "hideOnEscape": false,
                  "executeShortcut": "Ctrl+J"
                }
                """);

            var settings = AppSettingsStore.LoadOrDefault(dir);

            Assert.Equal(string.Empty, settings.ToggleVisibilityShortcut);
            Assert.Equal("Ctrl+J", settings.ExecuteShortcut);
            Assert.False(settings.HideOnEscape);
            var json = File.ReadAllText(Path.Combine(dir, AppSettingsStore.FileName));
            using var document = JsonDocument.Parse(json);
            Assert.Equal(
                string.Empty,
                document.RootElement.GetProperty("toggleVisibilityShortcut").GetString());
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
    [Fact]
    public void LoadOrDefault_NormalizesAllDisabledModesAndRewritesFile()
    {
        var dir = CreateTempDirectory();
        try
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(
                Path.Combine(dir, AppSettingsStore.FileName),
                """
                {
                  "modePreferences": [
                    { "key": "web-search", "enabled": false },
                    { "key": "wikipedia", "enabled": false },
                    { "key": "ask-ai", "enabled": false },
                    { "key": "dictionary", "enabled": false }
                  ]
                }
                """);

            var settings = AppSettingsStore.LoadOrDefault(dir);

            Assert.All(settings.ModePreferences, preference => Assert.True(preference.Enabled));
            var json = File.ReadAllText(Path.Combine(dir, AppSettingsStore.FileName));
            using var document = JsonDocument.Parse(json);
            var savedModes = document.RootElement
                .GetProperty("modePreferences")
                .EnumerateArray();
            Assert.All(savedModes, mode => Assert.True(mode.GetProperty("enabled").GetBoolean()));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private static string CreateTempDirectory()
        => Path.Combine(Path.GetTempPath(), "crispy-searchbar-tests-" + Guid.NewGuid().ToString("N"));
}
