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
using CrispySearchbar.Input;
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
    private bool _isShiftPressed;
    private ShortcutSettingFieldViewModel? _recordingShortcutField;
    private Control? _recordingShortcutButton;

    /// <summary>XAML/运行时创建入口；App 通常使用带占用探测委托的构造函数。</summary>
    public SettingsWindow()
        : this(null, null)
    {
    }

    public SettingsWindow(
        Func<string, bool>? globalShortcutProbe,
        Func<string, bool>? isCurrentGlobalShortcut)
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
            AppSettingsStore.GetSettingsFilePath(),
            globalShortcutProbe,
            isCurrentGlobalShortcut);
        viewModel.Saved += OnViewModelSaved;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        viewModel.ValidationFailed += OnValidationFailed;
        DataContext = viewModel;

        // 捕获期间用隧道拦截，避免聚焦的按钮把 Space/Enter 当作点击。
        AddHandler(KeyDownEvent, OnWindowKeyDown, RoutingStrategies.Tunnel);
        AddHandler(PointerPressedEvent, OnWindowPointerPressed, RoutingStrategies.Tunnel);
    }

    /// <summary>保存成功并写盘后触发，由 App 即时应用新配置。</summary>
    public event EventHandler<AppSettings>? SettingsSaved;

    private SettingsWindowViewModel ViewModel => (SettingsWindowViewModel)DataContext!;

    /// <summary>App 应用配置后用它把窗口刷新成磁盘上的新文件与新语言。</summary>
    public void ReloadFromConfiguration(AppSettings settings, AppStrings strings)
    {
        CancelShortcutRecording();
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

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (_recordingShortcutField is not null)
        {
            HandleShortcutRecording(e);
            return;
        }

        if (e.Key is Key.LeftShift or Key.RightShift)
        {
            _isShiftPressed = true;
        }
    }

    private void HandleShortcutRecording(KeyEventArgs e)
    {
        var field = _recordingShortcutField;
        if (field is null)
        {
            return;
        }

        if (e.Key == Key.Escape && e.KeyModifiers == KeyModifiers.None)
        {
            field.ClearToUnset();
            CancelShortcutRecording();
            e.Handled = true;
            ViewModel.RevalidateShortcuts();
            return;
        }

        if (ShortcutInputMapper.TryCreate(e.Key, e.KeyModifiers, out var candidate))
        {
            if (field.TrySetCaptured(candidate.ToStorageString()))
            {
                CancelShortcutRecording();
                ViewModel.RevalidateShortcuts();
            }
        }

        // 修饰键单独按下、系统键或尚未完成组合前都保持在“录制”状态，且不落到其它控件。
        e.Handled = true;
    }

    private void OnShortcutCaptureClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: ShortcutSettingFieldViewModel field } button)
        {
            return;
        }

        if (_recordingShortcutField == field)
        {
            field.CancelRecording();
            CancelShortcutRecording();
        }
        else
        {
            CancelShortcutRecording();
            field.BeginRecording();
            _recordingShortcutField = field;
            _recordingShortcutButton = button;
            ShortcutCaptureFocusHost.Focus();
        }
    }

    private void OnShortcutResetClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ShortcutSettingFieldViewModel field })
        {
            if (_recordingShortcutField == field)
            {
                CancelShortcutRecording();
            }

            field.ResetToDefault();
            ViewModel.RevalidateShortcuts();
        }
    }

    private void OnResetAllShortcutsClicked(object? sender, RoutedEventArgs e)
        => ViewModel.ResetAllShortcuts();

    private void OnResetSearchBarPositionClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: OffsetSettingFieldViewModel field })
        {
            field.ResetToDefault();
        }
    }

    private void OnWindowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var field = _recordingShortcutField;
        if (field is null)
        {
            return;
        }

        var properties = e.GetCurrentPoint(this).Properties;
        if (properties.IsXButton1Pressed || properties.IsXButton2Pressed)
        {
            if (ShortcutInputMapper.TryCreatePointer(
                    e.KeyModifiers,
                    properties,
                    out var candidate)
                && field.TrySetCaptured(candidate.ToStorageString()))
            {
                CancelShortcutRecording();
                ViewModel.RevalidateShortcuts();
            }

            e.Handled = true;
            return;
        }

        if (_recordingShortcutButton is not { } button)
        {
            return;
        }

        var point = e.GetPosition(button);
        if (point.X < 0
            || point.Y < 0
            || point.X > button.Bounds.Width
            || point.Y > button.Bounds.Height)
        {
            field.CancelRecording();
            CancelShortcutRecording();
        }
    }

    private void CancelShortcutRecording()
    {
        _recordingShortcutField = null;
        _recordingShortcutButton = null;
    }

    private void OnWindowKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.LeftShift or Key.RightShift)
        {
            _isShiftPressed = false;
        }
    }

    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        _isShiftPressed = false;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        Deactivated += OnWindowDeactivated;
    }

    protected override void OnClosed(EventArgs e)
    {
        CancelShortcutRecording();
        Deactivated -= OnWindowDeactivated;
        _statusFadeTimer?.Stop();
        base.OnClosed(e);
    }

    private void HandleModeMoveClick(object? sender, bool isTop)
    {
        if (sender is not Button { DataContext: ModeListItemViewModel item })
        {
            return;
        }

        if (_isShiftPressed)
        {
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

    private void OnModeMoveUpClicked(object? sender, RoutedEventArgs e)
        => HandleModeMoveClick(sender, isTop: true);

    private void OnModeMoveDownClicked(object? sender, RoutedEventArgs e)
        => HandleModeMoveClick(sender, isTop: false);

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
        HideModeDropPreview();
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
        ShowModeDropPreview(owner, _dragInsertionSlot);
        e.DragEffects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void OnModeSurfaceDrop(object? sender, DragEventArgs e)
    {
        if (_modeDragSource is not { } source)
        {
            return;
        }

        source.Owner.DropAt(source, _dragInsertionSlot);
        HideModeDropPreview();
        _modeDragSource = null;
        e.DragEffects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void ShowModeDropPreview(
        ModeListSettingFieldViewModel owner,
        int insertionIndex)
    {
        var indicatorY = ComputeModeDropIndicatorY(owner, insertionIndex);
        var firstRow = _modeRowControls.Values.FirstOrDefault();
        if (firstRow is null)
        {
            ModeDropIndicator.IsVisible = false;
            return;
        }

        var rowOrigin = firstRow.TranslatePoint(default, this);
        var left = rowOrigin?.X - 2 ?? 0;
        var right = Math.Max(0, Bounds.Width - left - firstRow.Bounds.Width + 2);
        ModeDropIndicator.Margin = new Thickness(
            Math.Max(0, left),
            Math.Max(0, indicatorY - 1),
            right,
            0);
        ModeDropIndicator.IsVisible = true;
    }

    private void HideModeDropPreview()
        => ModeDropIndicator.IsVisible = false;

    private double ComputeModeDropIndicatorY(
        ModeListSettingFieldViewModel owner,
        int insertionIndex)
    {
        var rows = new List<(double Top, double Bottom)>();
        foreach (var item in owner.Items)
        {
            if (!_modeRowControls.TryGetValue(item, out var rowControl))
            {
                continue;
            }

            var origin = rowControl.TranslatePoint(default, this);
            if (origin is null)
            {
                continue;
            }

            var top = origin.Value.Y;
            rows.Add((top, top + rowControl.Bounds.Height));
        }

        if (rows.Count == 0)
        {
            return 0;
        }

        var slot = Math.Clamp(insertionIndex, 0, rows.Count);
        if (slot == 0)
        {
            return rows[0].Top;
        }

        if (slot == rows.Count)
        {
            return rows[^1].Bottom;
        }

        return (rows[slot - 1].Bottom + rows[slot].Top) / 2;
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
