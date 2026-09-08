using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
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
    private const double ScrollWheelStep = 90;

    private readonly Dictionary<SettingsSectionViewModel, Control> _sectionControls = [];
    private readonly VectorTransition _scrollTransition;
    private ModeListItemViewModel? _modeDragSource;

    public SettingsWindow()
    {
        InitializeComponent();

        _scrollTransition = new VectorTransition
        {
            Property = ScrollViewer.OffsetProperty,
            Easing = new CubicEaseOut(),
        };
        PageScrollViewer.Transitions = new Transitions { _scrollTransition };
        SectionHost.PointerWheelChanged += OnPagePointerWheelChanged;

        var settings = AppSettingsStore.LoadOrDefault();
        var strings = TranslationCatalog.Default.Resolve(settings.Language);
        var viewModel = new SettingsWindowViewModel(
            settings,
            strings,
            AppSettingsStore.GetSettingsFilePath());
        viewModel.Saved += OnViewModelSaved;
        DataContext = viewModel;
    }

    /// <summary>保存成功并写盘后触发，由 App 即时应用新配置。</summary>
    public event EventHandler<AppSettings>? SettingsSaved;

    private SettingsWindowViewModel ViewModel => (SettingsWindowViewModel)DataContext!;

    /// <summary>App 应用配置后用它把窗口刷新成磁盘上的新文件与新语言。</summary>
    public void ReloadFromConfiguration(AppSettings settings, AppStrings strings)
    {
        _sectionControls.Clear();
        ViewModel.Reload(settings, strings);
    }

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

        ScrollSectionIntoView(section);
    }

    private void ScrollSectionIntoView(SettingsSectionViewModel section)
    {
        if (!_sectionControls.TryGetValue(section, out var anchor))
        {
            return;
        }

        var topInContent = anchor.TranslatePoint(default, SectionHost);
        if (topInContent is null)
        {
            return;
        }

        // 让分类标题在顶部保留 8 DIP 呼吸空间；内容不足时夹到最大滚动位置（即滚到底）。
        var target = Math.Clamp(topInContent.Value.Y - 8, 0, MaxVerticalOffset);
        AnimateVerticalOffsetTo(target);
    }

    private double MaxVerticalOffset
        => Math.Max(0, PageScrollViewer.Extent.Height - PageScrollViewer.Viewport.Height);

    private void AnimateVerticalOffsetTo(double target)
    {
        if (!PageScrollViewer.IsVisible)
        {
            return;
        }

        target = Math.Clamp(target, 0, MaxVerticalOffset);
        var current = PageScrollViewer.Offset.Y;
        if (Math.Abs(current - target) < 0.5)
        {
            PageScrollViewer.Offset = PageScrollViewer.Offset.WithY(target);
            return;
        }

        var distance = Math.Abs(target - current);
        _scrollTransition.Duration = TimeSpan.FromMilliseconds(Math.Clamp(140 + distance * 0.5, 140, 420));
        PageScrollViewer.Offset = PageScrollViewer.Offset.WithY(target);
    }

    private void OnPagePointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (PageScrollViewer.Extent.Height <= PageScrollViewer.Viewport.Height + 0.5)
        {
            return;
        }

        if (e.KeyModifiers == KeyModifiers.Shift)
        {
            // 本面板没有水平滚动，吃掉 Shift+滚轮避免外露水平原生行为。
            e.Handled = true;
            return;
        }

        if (Math.Abs(e.Delta.Y) < double.Epsilon)
        {
            return;
        }

        var target = Math.Clamp(
            PageScrollViewer.Offset.Y - e.Delta.Y * ScrollWheelStep,
            0,
            MaxVerticalOffset);
        e.Handled = true;
        AnimateVerticalOffsetTo(target);
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

        var options = new FilePickerOpenOptions
        {
            Title = field.BrowseText,
            AllowMultiple = false,
        };
        if (field.HasFileTypeFilter)
        {
            options.FileTypeFilter =
            [
                new FilePickerFileType(field.FileTypeFilterName ?? field.Label)
                {
                    Patterns = field.FileTypePatterns.ToArray(),
                },
            ];
        }

        var files = await StorageProvider.OpenFilePickerAsync(options);
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

    private void OnConfigPathTapped(object? sender, TappedEventArgs e)
        => BrowserLauncher.OpenFileWithDefaultApplication(ViewModel.ConfigFilePath);

    private void OnAboutLinkTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control { DataContext: AboutLinkViewModel link })
        {
            if (link.OpenAsFile)
            {
                BrowserLauncher.OpenFileWithDefaultApplication(link.Target);
            }
            else
            {
                BrowserLauncher.Open(link.Target);
            }
        }
    }

    private void OnResetConfigClicked(object? sender, RoutedEventArgs e)
        => ViewModel.ShowResetConfirmation();

    private void OnResetConfirmedClicked(object? sender, RoutedEventArgs e)
        => ViewModel.TryResetConfiguration();

    private void OnResetCancelledClicked(object? sender, RoutedEventArgs e)
        => ViewModel.HideResetConfirmation();

    private void OnModeMoveUpClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ModeListItemViewModel item })
        {
            item.MoveUp();
        }
    }

    private void OnModeMoveDownClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ModeListItemViewModel item })
        {
            item.MoveDown();
        }
    }

    private async void OnModeDragHandlePointerPressed(
        object? sender,
        PointerPressedEventArgs e)
    {
        if (sender is not Control { DataContext: ModeListItemViewModel item } handle)
        {
            return;
        }

        if (!e.GetCurrentPoint(handle).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _modeDragSource = item;
        var data = new DataTransfer();
        data.Add(DataTransferItem.CreateText(item.Key));
        await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Move);
        _modeDragSource = null;
    }

    private void OnModeRowDragOver(object? sender, DragEventArgs e)
    {
        if (_modeDragSource is not null
            && sender is Control { DataContext: ModeListItemViewModel })
        {
            e.DragEffects = DragDropEffects.Move;
            e.Handled = true;
            return;
        }

        e.DragEffects = DragDropEffects.None;
    }

    private void OnModeRowDrop(object? sender, DragEventArgs e)
    {
        if (sender is Control { DataContext: ModeListItemViewModel target }
            && _modeDragSource is not null)
        {
            target.Owner.DropOnto(_modeDragSource, target);
            e.Handled = true;
        }
    }
}
