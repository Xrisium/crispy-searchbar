using CrispySearchbar.Core.Configuration;
using CrispySearchbar.Core.Localization;
using CrispySearchbar.ViewModels;
using Xunit;

namespace CrispySearchbar.Ui.Tests;

/// <summary>
/// 自启设置写盘前的注册同步：系统注册失败时不保存配置，并显示可重试的本地化错误。
/// </summary>
public class StartupRegistrationSettingTests
{
    [Fact]
    public void Save_WhenRegistrationFails_DoesNotReportSuccess()
    {
        bool? requestedState = null;
        var strings = TranslationCatalog.Default.Resolve(AppLanguage.English);
        var viewModel = CreateViewModel(
            new AppSettings { LaunchAtStartup = false },
            strings,
            enabled =>
            {
                requestedState = enabled;
                return false;
            });
        FindLaunchField(viewModel).IsChecked = true;

        Assert.False(viewModel.TrySave());
        Assert.True(requestedState);
        Assert.Equal(
            strings.SettingsTexts.StartupRegistrationFailed,
            viewModel.StatusText);
    }

    [Fact]
    public void Save_AfterRegistrationFailure_RetriesInsteadOfWritingConfiguration()
    {
        var attempts = 0;
        var strings = TranslationCatalog.Default.Resolve(AppLanguage.English);
        var viewModel = CreateViewModel(
            new AppSettings { LaunchAtStartup = false },
            strings,
            _ =>
            {
                attempts++;
                return false;
            });
        FindLaunchField(viewModel).IsChecked = true;

        Assert.False(viewModel.TrySave());
        Assert.False(viewModel.TrySave());
        Assert.Equal(2, attempts);
    }

    [Fact]
    public void Save_WhenDisablingRegistration_RequestsRemoval()
    {
        bool? requestedState = null;
        var strings = TranslationCatalog.Default.Resolve(AppLanguage.English);
        var viewModel = CreateViewModel(
            new AppSettings { LaunchAtStartup = true },
            strings,
            enabled =>
            {
                requestedState = enabled;
                return false;
            });
        FindLaunchField(viewModel).IsChecked = false;

        Assert.False(viewModel.TrySave());
        Assert.False(requestedState);
        Assert.Equal(
            strings.SettingsTexts.StartupRegistrationFailed,
            viewModel.StatusText);
    }

    [Fact]
    public void Reset_WhenRegistrationFails_HidesConfirmationAndReportsError()
    {
        var strings = TranslationCatalog.Default.Resolve(AppLanguage.English);
        var viewModel = CreateViewModel(
            new AppSettings { LaunchAtStartup = true },
            strings,
            _ => false);
        viewModel.ShowResetConfirmation();

        Assert.False(viewModel.TryResetConfiguration());
        Assert.Equal(
            strings.SettingsTexts.StartupRegistrationFailed,
            viewModel.StatusText);
        Assert.False(viewModel.IsResetConfirmationVisible);
    }

    private static SettingsWindowViewModel CreateViewModel(
        AppSettings settings,
        AppStrings strings,
        Func<bool, bool> startupRegistrationUpdater)
        => new(
            settings,
            strings,
            AppSettingsStore.GetSettingsFilePath(),
            globalShortcutProbe: null,
            isCurrentGlobalShortcut: null,
            startupRegistrationUpdater);

    private static ToggleSettingFieldViewModel FindLaunchField(
        SettingsWindowViewModel viewModel)
        => viewModel.Sections
            .SelectMany(section => section.Fields)
            .OfType<ToggleSettingFieldViewModel>()
            .Single(field => field.Definition.PropertyName
                == nameof(AppSettings.LaunchAtStartup));
}
