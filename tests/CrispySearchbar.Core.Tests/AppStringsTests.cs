using System.Globalization;
using System.Text.Json.Nodes;
using CrispySearchbar.Core.Configuration;
using CrispySearchbar.Core.Localization;
using Xunit;

namespace CrispySearchbar.Core.Tests;

public class AppStringsTests
{
    [Theory]
    [InlineData(AppLanguage.SimplifiedChinese)]
    [InlineData(AppLanguage.English)]
    public void BundledLanguages_ReturnCompleteTexts(string language)
    {
        var strings = TranslationCatalog.Default.Resolve(language);

        Assert.Equal(language, strings.Language);
        Assert.False(string.IsNullOrWhiteSpace(strings.NativeName));
        Assert.False(string.IsNullOrWhiteSpace(strings.WikipediaLanguageCode));
        Assert.False(string.IsNullOrWhiteSpace(strings.ShowHideSearchBar));
        Assert.False(string.IsNullOrWhiteSpace(strings.Exit));
        Assert.False(string.IsNullOrWhiteSpace(strings.TrayToolTip));

        Assert.False(string.IsNullOrWhiteSpace(strings.WebSearchMode.Title));
        Assert.False(string.IsNullOrWhiteSpace(strings.WebSearchMode.Watermark));
        Assert.False(string.IsNullOrWhiteSpace(strings.WebSearchMode.ActionHint));
        Assert.False(string.IsNullOrWhiteSpace(strings.WikipediaMode.Title));
        Assert.False(string.IsNullOrWhiteSpace(strings.WikipediaMode.Watermark));
        Assert.False(string.IsNullOrWhiteSpace(strings.WikipediaMode.ActionHint));
        Assert.False(string.IsNullOrWhiteSpace(strings.AskAiMode.Title));
        Assert.False(string.IsNullOrWhiteSpace(strings.AskAiMode.Watermark));
        Assert.False(string.IsNullOrWhiteSpace(strings.AskAiMode.ActionHint));
        Assert.False(string.IsNullOrWhiteSpace(strings.DictionaryMode.Title));
        Assert.False(string.IsNullOrWhiteSpace(strings.DictionaryMode.Watermark));
        Assert.False(string.IsNullOrWhiteSpace(strings.DictionaryMode.ActionHint));

        Assert.False(string.IsNullOrWhiteSpace(strings.DictionaryEmptyHint));
        Assert.False(string.IsNullOrWhiteSpace(strings.DictionaryLoadingHint));
        Assert.False(string.IsNullOrWhiteSpace(strings.DictionaryDataSourceNotConfigured));
        Assert.Contains("{0}", strings.DictionaryNoResultsTemplate);
        Assert.Contains("{0}", strings.DictionaryLoadFailedTemplate);
        Assert.Contains("{0}", strings.DictionaryReadFailedTemplate);
        Assert.Contains("{0}", strings.ConfiguredDictionaryFileMissingTemplate);
        Assert.Contains("{0}", strings.DictionaryDataFileNotFoundTemplate);
    }

    [Fact]
    public void SystemLanguage_FollowsUiCultureWithEnglishFallback()
    {
        var catalog = TranslationCatalog.Default;

        Assert.Equal(
            AppLanguage.English,
            catalog.Resolve(AppLanguage.System, new CultureInfo("en-US")).Language);
        Assert.Equal(
            AppLanguage.SimplifiedChinese,
            catalog.Resolve(AppLanguage.System, new CultureInfo("zh-CN")).Language);
        Assert.Equal(
            AppLanguage.SimplifiedChinese,
            catalog.Resolve(AppLanguage.System, new CultureInfo("zh-TW")).Language);
        Assert.Equal(
            AppLanguage.English,
            catalog.Resolve(AppLanguage.System, new CultureInfo("fr-FR")).Language);
        Assert.Equal(
            AppLanguage.English,
            catalog.Resolve(null, new CultureInfo("de-DE")).Language);
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("unknown")]
    [InlineData("zh-CN")]
    public void UnsupportedConfiguredLanguage_FallsBackToEnglish(string language)
    {
        Assert.Equal(
            AppLanguage.English,
            TranslationCatalog.Default.Resolve(language).Language);
    }

    [Fact]
    public void Resolution_IsCaseInsensitive()
    {
        var catalog = TranslationCatalog.Default;

        Assert.Equal(AppLanguage.English, catalog.Resolve("EN").Language);
        Assert.Equal(AppLanguage.SimplifiedChinese, catalog.Resolve("ZH-HANS").Language);
    }

    [Fact]
    public void PartialTranslation_FallsBackToEnglishForMissingKeys()
    {
        var sources = TranslationCatalog.ReadEmbeddedSources(
            typeof(TranslationCatalog).Assembly);
        var partial = JsonNode.Parse(sources[AppLanguage.English])!.AsObject();

        partial["nativeName"] = "Deutsch";
        partial["wikipediaLanguage"] = "de";
        partial["dictionaryMode"]!["title"] = "Wörterbuch";
        partial["dictionaryMode"]!["watermark"] = "Deutsches Wort eingeben";
        partial.Remove("dictionaryLoadingHint");

        sources["de"] = partial.ToJsonString();
        var catalog = TranslationCatalog.Create(sources);
        var german = catalog.Resolve("de");

        Assert.Equal("de", german.Language);
        Assert.Equal("Deutsch", german.NativeName);
        Assert.Equal("de", german.WikipediaLanguageCode);
        Assert.Equal("Wörterbuch", german.DictionaryMode.Title);
        Assert.Equal(
            "Loading dictionary data…",
            german.DictionaryLoadingHint);
    }

    [Fact]
    public void AddedTranslationFile_IsDiscoveredWithoutRegistration()
    {
        var sources = TranslationCatalog.ReadEmbeddedSources(
            typeof(TranslationCatalog).Assembly);
        var partial = JsonNode.Parse(sources[AppLanguage.English])!.AsObject();
        partial["nativeName"] = "Deutsch";
        partial["wikipediaLanguage"] = "de";

        sources["de"] = partial.ToJsonString();
        var catalog = TranslationCatalog.Create(sources);

        Assert.Contains("de", catalog.LanguageCodes);
        Assert.Equal("Deutsch", catalog.GetNativeName("de"));
        Assert.Equal(
            new[]
            {
                AppLanguage.System,
                AppLanguage.English,
                "de",
                AppLanguage.SimplifiedChinese,
            },
            catalog.UiLanguageOptionCodes);
    }

    [Fact]
    public void FormatDictionaryNoResults_EmbedsQuery()
    {
        var chinese = TranslationCatalog.Default.Resolve(AppLanguage.SimplifiedChinese)
            .FormatDictionaryNoResults("apple");
        Assert.Contains("apple", chinese);

        var english = TranslationCatalog.Default.Resolve(AppLanguage.English)
            .FormatDictionaryNoResults("apple");
        Assert.Contains("apple", english);
    }

    [Fact]
    public void SearchEngineDisplayNames_AreLocalized()
    {
        var chinese = TranslationCatalog.Default.Resolve(AppLanguage.SimplifiedChinese);
        var english = TranslationCatalog.Default.Resolve(AppLanguage.English);

        Assert.Equal("百度", chinese.GetSearchEngineDisplayName(SearchEngineKind.Baidu));
        Assert.Equal("必应", chinese.GetSearchEngineDisplayName(SearchEngineKind.Bing));
        Assert.Equal("Baidu", english.GetSearchEngineDisplayName(SearchEngineKind.Baidu));
        Assert.Equal("Bing", english.GetSearchEngineDisplayName(SearchEngineKind.Bing));
        Assert.Equal("Google", english.GetSearchEngineDisplayName(SearchEngineKind.Google));
    }

    [Fact]
    public void SettingsTexts_CoverEverySchemaFieldAndKnownOptionInBundledLanguages()
    {
        var chinese = TranslationCatalog.Default.Resolve(AppLanguage.SimplifiedChinese)
            .SettingsTexts;
        var english = TranslationCatalog.Default.Resolve(AppLanguage.English)
            .SettingsTexts;

        Assert.Equal(
            chinese.FieldLabels.Keys.OrderBy(key => key, StringComparer.Ordinal),
            english.FieldLabels.Keys.OrderBy(key => key, StringComparer.Ordinal));
        Assert.Equal(
            chinese.FieldDescriptions.Keys.OrderBy(key => key, StringComparer.Ordinal),
            english.FieldDescriptions.Keys.OrderBy(key => key, StringComparer.Ordinal));
        Assert.Equal(
            chinese.SectionTitles.Keys.OrderBy(key => key, StringComparer.Ordinal),
            english.SectionTitles.Keys.OrderBy(key => key, StringComparer.Ordinal));
        Assert.Equal(
            chinese.FileTypeFilterNames.Keys.OrderBy(key => key, StringComparer.Ordinal),
            english.FileTypeFilterNames.Keys.OrderBy(key => key, StringComparer.Ordinal));

        Assert.Equal("跟随系统", chinese.GetOptionLabel("Language", AppLanguage.System));
        Assert.Equal("System", english.GetOptionLabel("Language", AppLanguage.System));

        var languageOptions = TranslationCatalog.Default.UiLanguageOptionCodes;
        var definitions = AppSettingsSchema.Discover(languageOptions);
        foreach (var definition in definitions)
        {
            Assert.True(
                chinese.FieldLabels.ContainsKey(definition.PropertyName),
                $"缺少字段标签：{definition.PropertyName}");
            Assert.True(
                english.FieldLabels.ContainsKey(definition.PropertyName),
                $"缺少字段标签：{definition.PropertyName}");
            Assert.True(
                chinese.FieldDescriptions.ContainsKey(definition.PropertyName),
                $"缺少字段说明：{definition.PropertyName}");
            Assert.True(
                english.FieldDescriptions.ContainsKey(definition.PropertyName),
                $"缺少字段说明：{definition.PropertyName}");

            if (definition.PropertyName == nameof(AppSettings.Language))
            {
                foreach (var option in definition.OptionValues)
                {
                    if (string.Equals(
                            option?.ToString(),
                            AppLanguage.System,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    Assert.False(
                        string.IsNullOrWhiteSpace(
                            TranslationCatalog.Default.GetNativeName(option?.ToString() ?? string.Empty)),
                        $"语言缺少 nativeName：{option}");
                }

                continue;
            }

            foreach (var option in definition.OptionValues)
            {
                var key = definition.PropertyName + "." + option;
                Assert.True(chinese.OptionLabels.ContainsKey(key), $"缺少选项文案：{key}");
                Assert.True(english.OptionLabels.ContainsKey(key), $"缺少选项文案：{key}");
            }

            if (definition.EditorKind == SettingEditorKind.FilePath
                && !string.IsNullOrWhiteSpace(definition.FileTypeFilterKey))
            {
                Assert.True(
                    chinese.FileTypeFilterNames.ContainsKey(definition.FileTypeFilterKey),
                    $"缺少文件类型文案：{definition.FileTypeFilterKey}");
                Assert.True(
                    english.FileTypeFilterNames.ContainsKey(definition.FileTypeFilterKey),
                    $"缺少文件类型文案：{definition.FileTypeFilterKey}");
                Assert.True(
                    definition.FileTypePatterns.Count > 0,
                    $"文件类型文案存在但缺少通配符：{definition.PropertyName}");
            }
        }

        foreach (var section in Enum.GetValues<SettingsSection>())
        {
            Assert.True(chinese.SectionTitles.ContainsKey(section.ToString()), $"缺少分类标题：{section}");
            Assert.True(english.SectionTitles.ContainsKey(section.ToString()), $"缺少分类标题：{section}");
        }
    }
}
