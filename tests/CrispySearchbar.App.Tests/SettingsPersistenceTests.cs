using CrispySearchbar.Core.Configuration;
using CrispySearchbar.Core.Localization;
using CrispySearchbar.ViewModels;
using Xunit;

namespace CrispySearchbar.Ui.Tests;

public sealed class SettingsPersistenceTests
{
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
