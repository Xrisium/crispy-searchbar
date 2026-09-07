using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CrispySearchbar.Core.Configuration;
using CrispySearchbar.Core.Localization;
using CrispySearchbar.ViewModels;

namespace CrispySearchbar;

/// <summary>
/// 配置文件的可视化编辑器。窗口自身不缓存配置来源，只负责读文件、编辑并写回。
/// </summary>
public sealed partial class SettingsWindow : Window
{
    private readonly Dictionary<SettingsSectionViewModel, Control> _sectionControls = [];

    public SettingsWindow()
    {
        InitializeComponent();

        var settings = AppSettingsStore.LoadOrDefault();
        var strings = AppStrings.For(settings.Language);
        var viewModel = new SettingsWindowViewModel(
            settings,
            strings.SettingsTexts,
            AppSettingsStore.GetSettingsFilePath());
        viewModel.Saved += OnViewModelSaved;
        DataContext = viewModel;
    }

    /// <summary>保存成功并写盘后触发，由 App 即时应用新配置。</summary>
    public event EventHandler<AppSettings>? SettingsSaved;

    private SettingsWindowViewModel ViewModel => (SettingsWindowViewModel)DataContext!;

    /// <summary>App 应用配置后用它把窗口刷新成磁盘上的新文件与新语言。</summary>
    public void ReloadFromConfiguration(AppSettings settings, AppStrings strings)
        => ViewModel.Reload(settings, strings.SettingsTexts);

    private void OnViewModelSaved(object? sender, AppSettings settings)
        => SettingsSaved?.Invoke(this, settings);

    private void OnSaveClicked(object? sender, RoutedEventArgs e)
        => ViewModel.TrySave();

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
        => Close();

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void OnCategorySelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (CategoryNav.SelectedItem is not SettingsSectionViewModel section)
        {
            return;
        }

        if (_sectionControls.TryGetValue(section, out var container))
        {
            container.BringIntoView();
        }
    }

    private void OnSectionLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: SettingsSectionViewModel section } control)
        {
            _sectionControls[section] = control;
        }
    }

    private async void OnBrowsePathClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: FilePathSettingFieldViewModel field })
        {
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = field.BrowseText,
            AllowMultiple = false,
        });
        if (files.Count > 0)
        {
            field.FilePath = files[0].TryGetLocalPath();
        }
    }

    private void OnClearPathClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: FilePathSettingFieldViewModel field })
        {
            field.ClearPath();
        }
    }
}
