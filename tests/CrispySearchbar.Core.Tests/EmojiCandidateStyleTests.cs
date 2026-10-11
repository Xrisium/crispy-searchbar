using CrispySearchbar.Core.Configuration;
using CrispySearchbar.Core.Emoji;
using CrispySearchbar.Core.Localization;
using Xunit;

namespace CrispySearchbar.Core.Tests;

public sealed class EmojiCandidateStyleTests
{
    [Theory]
    [InlineData("{}", EmojiCandidateStyle.List)]
    [InlineData("{\"emojiCandidateStyle\":\"grid\"}", EmojiCandidateStyle.Grid)]
    [InlineData("{\"emojiCandidateStyle\":99}", EmojiCandidateStyle.List)]
    public void StyleLoadsLegacyConfigurationAndRoundTrips(string json, EmojiCandidateStyle expected)
    {
        var directory = Path.Combine(Path.GetTempPath(), "crispy-emoji-style-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, AppSettingsStore.FileName), json);
            var settings = AppSettingsStore.LoadOrDefault(directory);
            Assert.Equal(expected, settings.EmojiCandidateStyle);
            settings.EmojiCandidateStyle = EmojiCandidateStyle.Grid;
            AppSettingsStore.Save(settings, directory);
            Assert.Equal(EmojiCandidateStyle.Grid, AppSettingsStore.LoadOrDefault(directory).EmojiCandidateStyle);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData("en")]
    [InlineData("zh-Hans")]
    public void EmbeddedCategoriesAndStyleOptionsAreLocalized(string language)
    {
        var strings = TranslationCatalog.Default.Resolve(language);
        var groups = EmojiResources.LoadIndex().Entries.Select(e => e.Group).Distinct().ToArray();
        Assert.Equal(9, groups.Length);
        Assert.All(groups, key => Assert.True(strings.Emoji.Categories.ContainsKey(key)));
        var field = AppSettingsSchema.Discover().Single(d => d.PropertyName == nameof(AppSettings.EmojiCandidateStyle));
        Assert.Equal(SettingsSection.Appearance, field.Section);
        Assert.Equal(SettingEditorKind.Choice, field.EditorKind);
        Assert.NotEqual(field.PropertyName, strings.SettingsTexts.GetFieldLabel(field.PropertyName));
        Assert.All(field.OptionValues, option => Assert.True(strings.SettingsTexts.OptionLabels.ContainsKey(field.PropertyName + "." + option)));
    }
}
