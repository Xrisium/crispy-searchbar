using CrispySearchbar.Core.Configuration;
using CrispySearchbar.Core.Localization;
using CrispySearchbar.ViewModels;
using Xunit;

namespace CrispySearchbar.Ui.Tests;

public sealed class SettingsPersistenceTests
{
    [Fact]
    public void EmojiStyleChoiceSavesAndResetRestoresList()
    {
        var directory = Path.Combine(Path.GetTempPath(), "crispy-emoji-settings-" + Guid.NewGuid().ToString("N"));
        try
        {
            var vm = new SettingsWindowViewModel(new AppSettings(),
                TranslationCatalog.Default.Resolve("zh-Hans"), AppSettingsStore.GetSettingsFilePath(directory));
            var field = Assert.IsType<ChoiceSettingFieldViewModel>(vm.Sections.SelectMany(s => s.Fields)
                .Single(f => f.Definition.PropertyName == nameof(AppSettings.EmojiCandidateStyle)));
            Assert.Equal(EmojiCandidateStyle.List, field.SelectedOption!.Value);
            field.SelectedOption = field.Options.Single(o => Equals(o.Value, EmojiCandidateStyle.Grid));
            Assert.True(vm.TrySave());
            Assert.Equal(EmojiCandidateStyle.Grid, AppSettingsStore.LoadOrDefault(directory).EmojiCandidateStyle);
            Assert.True(vm.TryResetConfiguration());
            Assert.Equal(EmojiCandidateStyle.List, AppSettingsStore.LoadOrDefault(directory).EmojiCandidateStyle);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void SavingAndResettingOnlyWriteTheDisplayedConfigurationFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "crispy-settings-ui-" + Guid.NewGuid().ToString("N"));
        var current = Path.Combine(root, "current");
        var other = Path.Combine(root, "legacy");
        try
        {
            AppSettingsStore.Save(new AppSettings { Theme = ThemePreference.Light }, other);
            var untouched = File.ReadAllBytes(AppSettingsStore.GetSettingsFilePath(other));
            var vm = new SettingsWindowViewModel(new AppSettings { Theme = ThemePreference.Dark },
                TranslationCatalog.Default.Resolve("en"), AppSettingsStore.GetSettingsFilePath(current));
            Assert.True(vm.TrySave());
            Assert.Equal(ThemePreference.Dark, AppSettingsStore.LoadOrDefault(current).Theme);
            Assert.True(vm.TryResetConfiguration());
            Assert.Equal(ThemePreference.System, AppSettingsStore.LoadOrDefault(current).Theme);
            Assert.Equal(untouched, File.ReadAllBytes(AppSettingsStore.GetSettingsFilePath(other)));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
