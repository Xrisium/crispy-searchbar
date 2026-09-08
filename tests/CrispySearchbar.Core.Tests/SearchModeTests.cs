using CrispySearchbar.Core.Configuration;
using CrispySearchbar.Core.Localization;
using CrispySearchbar.Core.Modes;
using Xunit;

namespace CrispySearchbar.Core.Tests;

public class SearchModeTests
{
    [Fact]
    public void AskAi_UsesLocalizedAskAiTexts()
    {
        var strings = TranslationCatalog.Default.Resolve(AppLanguage.SimplifiedChinese);
        var formatted = ShortcutTextFormatter.Format(
            strings.AskAiMode,
            ShortcutCatalog.Default,
            strings.SettingsTexts.ShortcutUnsetText);
        var mode = SearchMode.AskAi(
            formatted,
            "https://chat.deepseek.com/?q={0}");

        Assert.Equal("ask-ai", mode.Key);
        Assert.Equal("问问大肥鱼", mode.Title);
        Assert.Equal("输入问题，按 Enter 跳转到 DeepSeek 网页端", mode.Watermark);
        Assert.Equal("按 Enter 跳转到 DeepSeek 网页端", mode.ActionHint);
    }

    [Fact]
    public void DefaultCatalog_KeepsDocumentedModeOrderAndChineseDisplayNames()
    {
        var settings = new AppSettings();
        var strings = TranslationCatalog.Default.Resolve(AppLanguage.SimplifiedChinese);
        var modes = SearchModeCatalog.Create(settings, strings);

        Assert.Equal(
            new[] { "web-search", "wikipedia", "ask-ai", "dictionary" },
            modes.Select(mode => mode.Key));
        Assert.Equal("网页搜索", modes[0].Title);
        Assert.Equal("维基百科", modes[1].Title);
        Assert.Equal("问问大肥鱼", modes[2].Title);
        Assert.Equal("词典", modes[3].Title);
    }

    [Fact]
    public void DefaultCatalog_UsesEnglishTextsWhenEnglishIsConfigured()
    {
        var settings = new AppSettings();
        var strings = TranslationCatalog.Default.Resolve(AppLanguage.English);
        var modes = SearchModeCatalog.Create(settings, strings);

        Assert.Equal("Web Search", modes[0].Title);
        Assert.Equal("Wikipedia", modes[1].Title);
        Assert.Equal("Ask DeepSeek", modes[2].Title);
        Assert.Equal("Dictionary", modes[3].Title);
        Assert.Equal(
            "Type an English word or Chinese term",
            modes[3].Watermark);
    }

    [Fact]
    public void Wikipedia_ModeTargetsSiteLanguageFromTranslationMetadata()
    {
        var settings = new AppSettings();

        var chineseStrings = TranslationCatalog.Default.Resolve(AppLanguage.SimplifiedChinese);
        var chineseModes = SearchModeCatalog.Create(settings, chineseStrings);
        Assert.Equal(
            "https://zh.wikipedia.org/w/index.php?search={0}",
            chineseModes[1].UrlTemplate);

        var englishStrings = TranslationCatalog.Default.Resolve(AppLanguage.English);
        var englishModes = SearchModeCatalog.Create(settings, englishStrings);
        Assert.Equal(
            "https://en.wikipedia.org/w/index.php?search={0}",
            englishModes[1].UrlTemplate);
    }

    [Fact]
    public void ConfiguredCatalog_SkipsDisabledModesAndKeepsStoredOrder()
    {
        var settings = new AppSettings
        {
            ModePreferences =
            [
                new ModePreference(ModePreferenceDefaults.Dictionary, enabled: true),
                new ModePreference(ModePreferenceDefaults.WebSearch, enabled: false),
                new ModePreference(ModePreferenceDefaults.Wikipedia, enabled: true),
                new ModePreference(ModePreferenceDefaults.AskAi, enabled: false),
            ],
        };
        var strings = TranslationCatalog.Default.Resolve(AppLanguage.SimplifiedChinese);

        var modes = SearchModeCatalog.Create(settings, strings);

        Assert.Equal(
            new[] { "dictionary", "wikipedia" },
            modes.Select(mode => mode.Key));
    }

    [Fact]
    public void Normalize_DropsUnknownAndDuplicateKeysAndAppendsMissingEnabledModes()
    {
        var normalized = ModePreferenceNormalizer.Normalize(
        [
            new ModePreference("future-mode", enabled: true),
            new ModePreference(ModePreferenceDefaults.WebSearch, enabled: true),
            new ModePreference(ModePreferenceDefaults.WebSearch, enabled: false),
            new ModePreference(ModePreferenceDefaults.Dictionary, enabled: false),
        ]);

        Assert.Equal(
            new[]
            {
                ModePreferenceDefaults.WebSearch,
                ModePreferenceDefaults.Dictionary,
                ModePreferenceDefaults.Wikipedia,
                ModePreferenceDefaults.AskAi,
            },
            normalized.Select(preference => preference.Key));
        Assert.True(normalized.Single(preference =>
            preference.Key == ModePreferenceDefaults.WebSearch).Enabled);
        Assert.True(normalized.Single(preference =>
            preference.Key == ModePreferenceDefaults.Wikipedia).Enabled);
        Assert.True(normalized.Single(preference =>
            preference.Key == ModePreferenceDefaults.AskAi).Enabled);
        Assert.False(normalized.Single(preference =>
            preference.Key == ModePreferenceDefaults.Dictionary).Enabled);
    }

    [Fact]
    public void Normalize_AllDisabledFallsBackToDefaultAllEnabled()
    {
        var normalized = ModePreferenceNormalizer.Normalize(
        [
            new ModePreference(ModePreferenceDefaults.WebSearch, enabled: false),
            new ModePreference(ModePreferenceDefaults.Wikipedia, enabled: false),
            new ModePreference(ModePreferenceDefaults.AskAi, enabled: false),
            new ModePreference(ModePreferenceDefaults.Dictionary, enabled: false),
        ]);

        Assert.All(normalized, preference => Assert.True(preference.Enabled));
        Assert.Equal(
            ModePreferenceDefaults.BuiltInOrder,
            normalized.Select(preference => preference.Key));
    }
}
