using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
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
    private const int StatusFadeDelayMs = 3000;

    private readonly Dictionary<SettingsSectionViewModel, Control> _sectionControls = [];
    private readonly Dictionary<SettingFieldViewModel, Control> _fieldControls = [];
    private readonly Dictionary<ModeListItemViewModel, Control> _modeRowControls = [];
    private readonly VectorTransition _scrollTransition;
    private DispatcherTimer? _statusFadeTimer;
    private ModeListItemViewModel? _modeDragSource;
    private int _dragInsertionSlot;
    private ModeListItemViewModel? _shiftClickItem;
    private bool _shiftClickIsTop;

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
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        viewModel.ValidationFailed += OnValidationFailed;
        DataContext = viewModel;
    }

    /// <summary>保存成功并写盘后触发，由 App 即时应用新配置。</summary>
    public event EventHandler<AppSettings>? SettingsSaved;

    private SettingsWindowViewModel ViewModel => (SettingsWindowViewModel)DataContext!;

    /// <summary>App 应用配置后用它把窗口刷新成磁盘上的新文件与新语言。</summary>
    public void ReloadFromConfiguration(AppSettings settings, AppStrings strings)
    {
        _sectionControls.Clear();
        _fieldControls.Clear();
        _modeRowControls.Clear();
        ViewModel.Reload(settings, strings);
    }

    private void OnViewModelSaved(object? sender, AppSettings settings)
        => SettingsSaved?.Invoke(this, settings);

    private void OnSaveClicked(object? sender, RoutedEventArgs e)
    {
        ViewModel.TrySave();
        RestartStatusFade();
    }

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

    private void ScrollSettingFieldIntoView(SettingFieldViewModel field)
    {
        if (!_fieldControls.TryGetValue(field, out var anchor))
        {
            return;
        }

        var topInContent = anchor.TranslatePoint(default, SectionHost);
        if (topInContent is null)
        {
            return;
        }

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

    private void OnSettingFieldLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: SettingFieldViewModel field } control)
        {
            _fieldControls[field] = control;
        }
    }

    private void OnModeRowLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: ModeListItemViewModel item } control)
        {
            _modeRowControls[item] = control;
        }
    }

    private void OnValidationFailed(object? sender, SettingFieldViewModel field)
        => ScrollSettingFieldIntoView(field);

    private void OnViewModelPropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsWindowViewModel.StatusText))
        {
            RestartStatusFade();
        }
    }

    private void RestartStatusFade()
    {
        _statusFadeTimer?.Stop();
        StatusTextBlock.Opacity = 1;

        _statusFadeTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(StatusFadeDelayMs),
        };
        _statusFadeTimer.Tick += OnStatusFadeTick;
        _statusFadeTimer.Start();
    }

    private void OnStatusFadeTick(object? sender, EventArgs e)
    {
        _statusFadeTimer?.Stop();
        StatusTextBlock.Opacity = 0;
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
    {
        ViewModel.TryResetConfiguration();
        RestartStatusFade();
    }

    private void OnResetCancelledClicked(object? sender, RoutedEventArgs e)
        => ViewModel.HideResetConfirmation();

    private void OnModeMoveUpButtonPointerPressed(
        object? sender,
        PointerPressedEventArgs e)
        => TrackShiftMove(sender, e, isTop: true);

    private void OnModeMoveDownButtonPointerPressed(
        object? sender,
        PointerPressedEventArgs e)
        => TrackShiftMove(sender, e, isTop: false);

    private void TrackShiftMove(
        object? sender,
        PointerEventArgs e,
        bool isTop)
    {
        if (sender is not Control { DataContext: ModeListItemViewModel item })
        {
            return;
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            _shiftClickItem = item;
            _shiftClickIsTop = isTop;
        }
        else
        {
            _shiftClickItem = null;
        }
    }

    private void OnModeMoveUpClicked(object? sender, RoutedEventArgs e)
        => HandleModeMoveClick(sender, isTop: true);

    private void OnModeMoveDownClicked(object? sender, RoutedEventArgs e)
        => HandleModeMoveClick(sender, isTop: false);

    private void HandleModeMoveClick(object? sender, bool isTop)
    {
        if (sender is not Button { DataContext: ModeListItemViewModel item })
        {
            return;
        }

        if (ReferenceEquals(_shiftClickItem, item) && _shiftClickIsTop == isTop)
        {
            _shiftClickItem = null;
            if (isTop)
            {
                item.MoveToTop();
            }
            else
            {
                item.MoveToBottom();
            }

            return;
        }

        if (isTop)
        {
            item.MoveUp();
        }
        else
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
        if (_modeDragSource is { } finishedSource)
        {
            finishedSource.Owner.SetDropPreviewSlot(null);
        }

        _modeDragSource = null;
    }

    private void OnModeSurfaceDragOver(object? sender, DragEventArgs e)
    {
        if (_modeDragSource is null)
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        var owner = _modeDragSource.Owner;
        _dragInsertionSlot = ComputeDropInsertionSlot(e, owner);
        owner.SetDropPreviewSlot(_dragInsertionSlot);
        e.DragEffects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void OnModeSurfaceDragLeave(object? sender, DragEventArgs e)
    {
        _modeDragSource?.Owner.SetDropPreviewSlot(null);
    }

    private void OnModeSurfaceDrop(object? sender, DragEventArgs e)
    {
        if (_modeDragSource is not { } source)
        {
            return;
        }

        source.Owner.DropAt(source, _dragInsertionSlot);
        source.Owner.SetDropPreviewSlot(null);
        _modeDragSource = null;
        e.DragEffects = DragDropEffects.Move;
        e.Handled = true;
    }

    private int ComputeDropInsertionSlot(
        DragEventArgs e,
        ModeListSettingFieldViewModel owner)
    {
        var pointerY = e.GetPosition(SettingsSurface).Y;
        var items = owner.Items;
        for (var index = 0; index < items.Count; index++)
        {
            if (!_modeRowControls.TryGetValue(items[index], out var rowControl))
            {
                continue;
            }

            var rowTop = rowControl.TranslatePoint(default, SettingsSurface);
            if (rowTop is null)
            {
                continue;
            }

            var rowCenterY = rowTop.Value.Y + rowControl.Bounds.Height / 2;
            if (pointerY < rowCenterY)
            {
                return index;
            }
        }

        return items.Count;
    }
}
