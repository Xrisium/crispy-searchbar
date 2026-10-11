using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CrispySearchbar.Core.Configuration;
using CrispySearchbar.Core.Emoji;
using CrispySearchbar.Core.Localization;
using CrispySearchbar.Core.Modes;
using CrispySearchbar.Emoji;
using CrispySearchbar.Platform;
using CrispySearchbar.ViewModels;
using Xunit;

namespace CrispySearchbar.Ui.Tests;

public sealed class EmojiInteractionTests
{
    private static readonly Task<EmojiIndex> Index = Task.Run(EmojiResources.LoadIndex);
    private sealed class FakeInput : IEmojiTextInputService
    {
        public List<string> Sent { get; } = [];
        public List<ExternalInputTarget?> Targets { get; } = [];
        public ExternalInputTarget? TargetToCapture { get; set; } = new(new IntPtr(42), 42, new IntPtr(43));
        public bool Succeed { get; set; } = true;
        public ExternalInputTarget? CaptureTarget() => TargetToCapture;
        public Task<bool> InsertAsync(ExternalInputTarget? target, string text, CancellationToken cancellationToken)
        {
            Sent.Add(text);
            Targets.Add(target);
            return Task.FromResult(Succeed && target is not null);
        }
    }
    private sealed class Fixture : IDisposable
    {
        public string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), "crispy-emoji-ui-" + Guid.NewGuid().ToString("N"));
        public MainWindow Window
        {
            get;
        }
        public MainWindowViewModel Main
        {
            get;
        }
        public EmojiModeViewModel Emoji
        {
            get;
        }
        public FakeInput Input { get; } = new();
        public Fixture(bool keepOpen = false, EmojiCandidateStyle style = EmojiCandidateStyle.List)
        {
            TestStyles.Ensure();
            var settings = new AppSettings { EmojiKeepOpenAfterInsert = keepOpen, EmojiCandidateStyle = style };
            var strings = TranslationCatalog.Default.Resolve("zh-Hans");
            Emoji = new(strings, settings, Index, new EmojiHistoryStore(DirectoryPath), new EmojiImageCache());
            Main = new(strings, [SearchMode.Emoji(strings.EmojiMode), SearchMode.WebSearch(strings.WebSearchMode, "https://example.com?q={0}")], _ => { }, emoji: Emoji);
            Window = new MainWindow { DataContext = Main };
            Window.AttachEmojiInput(Input);
            Window.ShowFromTray();
        }
        public void Dispose()
        {
            Window.AllowClose();
            Window.Close();
            Emoji.Dispose();
            if (Directory.Exists(DirectoryPath))
                Directory.Delete(DirectoryPath, true);
        }
    }
    private static async Task Ready(Fixture f, string query)
    {
        f.Main.Query = query;
        for (var i = 0; i < 300 && f.Emoji.Candidates.Count == 0; i++)
            await Task.Delay(10);
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
        await Task.Delay(30);
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
        Assert.NotEmpty(f.Emoji.Candidates);
    }

    private static async Task SearchFinished(Fixture f, string query)
    {
        f.Main.Query = query;
        var loading = TranslationCatalog.Default.Resolve("zh-Hans").Emoji.Loading;
        for (var i = 0; i < 500 && f.Emoji.Status == loading; i++)
            await Task.Delay(10);
        Assert.NotEqual(loading, f.Emoji.Status);
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
    }

    [AvaloniaFact]
    public async Task GridBrowsesEveryCategoryWithoutTruncatingAndKeepsInputFocused()
    {
        using var f = new Fixture(style: EmojiCandidateStyle.Grid);
        await SearchFinished(f, "");
        Assert.True(f.Emoji.IsGrid);
        Assert.Equal(10, f.Emoji.Categories.Count);
        Assert.Equal("最近使用", f.Emoji.Categories[0].Title);
        Assert.Equal(1, f.Emoji.SelectedCategoryIndex);
        Assert.Equal(1, f.Window.FindControl<ListBox>("EmojiCategoriesList")!.SelectedIndex);
        var index = await Index;
        var browsed = new HashSet<string>();
        for (var i = 1; i < f.Emoji.Categories.Count; i++)
        {
            f.Emoji.SelectedCategoryIndex = i;
            Assert.All(f.Emoji.Candidates, c => Assert.Equal(f.Emoji.Categories[i].Key, c.Entry.Group));
            browsed.UnionWith(f.Emoji.Candidates.Select(c => c.Entry.VariantGroup));
        }
        Assert.Equal(index.Entries.Select(e => e.VariantGroup).Distinct().Order(), browsed.Order());
        Assert.True(f.Window.FindControl<TextBox>("QueryBox")!.IsFocused);
        Assert.True(f.Window.FindControl<ListBox>("EmojiGridList")!.IsVisible);
        Assert.False(f.Window.FindControl<ListBox>("EmojiCandidatesList")!.IsVisible);
    }

    [AvaloniaFact]
    public async Task TypingFocusesAllResultsAndCategoriesFilterTheCompleteSearch()
    {
        using var f = new Fixture(style: EmojiCandidateStyle.Grid);
        await SearchFinished(f, "");
        f.Emoji.SelectedCategoryIndex = 3;
        await SearchFinished(f, "heart");
        Assert.Equal("搜索结果", f.Emoji.Categories[0].Title);
        Assert.Equal(0, f.Emoji.SelectedCategoryIndex);
        Assert.Equal(0, f.Window.FindControl<ListBox>("EmojiCategoriesList")!.SelectedIndex);
        var expected = (await Index).Search("heart", maxResults: int.MaxValue).Select(m => m.Entry.Sequence).ToArray();
        Assert.True(expected.Length > 12);
        Assert.Equal(expected, f.Emoji.Candidates.Select(c => c.Entry.Sequence));
        var union = new HashSet<string>();
        for (var i = 1; i < f.Emoji.Categories.Count; i++)
        {
            f.Emoji.SelectedCategoryIndex = i;
            Assert.All(f.Emoji.Candidates, c => Assert.Equal(f.Emoji.Categories[i].Key, c.Entry.Group));
            union.UnionWith(f.Emoji.Candidates.Select(c => c.Entry.Sequence));
        }
        Assert.Equal(expected.Order(), union.Order());
        await SearchFinished(f, "fire");
        Assert.Equal(0, f.Emoji.SelectedCategoryIndex);
        Assert.Equal(0, f.Window.FindControl<ListBox>("EmojiCategoriesList")!.SelectedIndex);
        await SearchFinished(f, "zzzzzzzzzzzzzzzzzz");
        Assert.Empty(f.Emoji.Candidates);
        Assert.True(f.Emoji.HasStatus);
    }

    [AvaloniaFact]
    public async Task RecentContainsActualHistoryAndClearQueryRestoresRecentTab()
    {
        using var f = new Fixture(style: EmojiCandidateStyle.Grid);
        await SearchFinished(f, "");
        f.Emoji.SelectedCategoryIndex = 0;
        Assert.Empty(f.Emoji.Candidates);
        Assert.Equal(TranslationCatalog.Default.Resolve("zh-Hans").Emoji.NoRecent, f.Emoji.Status);
        await f.Emoji.RecordAsync("👍🏿");
        await f.Emoji.RecordAsync("☕");
        await SearchFinished(f, "heart");
        await SearchFinished(f, " ");
        Assert.Equal("最近使用", f.Emoji.Categories[0].Title);
        Assert.Equal(0, f.Emoji.SelectedCategoryIndex);
        Assert.Equal(new[] { "☕", "👍🏿" }, f.Emoji.Candidates.Select(c => c.Entry.Sequence));
    }

    [AvaloniaFact]
    public async Task GridKeyboardMovesByRowAndSwitchesTabsAndPreservesVariants()
    {
        using var f = new Fixture(style: EmojiCandidateStyle.Grid);
        await SearchFinished(f, "smile");
        f.Emoji.SelectedIndex = 5;
        f.Window.KeyPress(Key.Up, RawInputModifiers.None, PhysicalKey.ArrowUp, "");
        Assert.Equal(5, f.Emoji.SelectedIndex);
        f.Emoji.SelectedIndex = 0;
        f.Window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, "");
        Assert.Equal(EmojiModeViewModel.GridColumns, f.Emoji.SelectedIndex);
        f.Window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, "");
        Assert.Equal(EmojiModeViewModel.GridColumns + 1, f.Emoji.SelectedIndex);
        f.Window.KeyPress(Key.Left, RawInputModifiers.Alt, PhysicalKey.ArrowLeft, "");
        Assert.Equal(f.Emoji.Categories.Count - 1, f.Emoji.SelectedCategoryIndex);
        Assert.True(f.Window.FindControl<TextBox>("QueryBox")!.IsFocused);
        await SearchFinished(f, "dianzan");
        f.Emoji.OpenVariants();
        Assert.False(f.Emoji.ShowCategories);
        f.Emoji.SelectedIndex = 5;
        Assert.True(f.Emoji.CommitVariant());
        Assert.True(f.Emoji.ShowCategories);
        Assert.Equal("👍🏿", f.Emoji.Selected!.Entry.Sequence);
        Assert.Equal(0, f.Emoji.SelectedCategoryIndex);
    }

    [AvaloniaFact]
    public async Task StyleCanSwitchLiveAndGridClickUsesExistingInputPath()
    {
        using var f = new Fixture();
        await SearchFinished(f, "heart");
        Assert.Equal(12, f.Emoji.Candidates.Count);
        var strings = TranslationCatalog.Default.Resolve("zh-Hans");
        f.Emoji.ApplyConfiguration(strings, new AppSettings { EmojiCandidateStyle = EmojiCandidateStyle.Grid });
        await SearchFinished(f, "heart");
        Assert.True(f.Emoji.Candidates.Count > 12);
        f.Window.Height = 600;
        f.Window.FindControl<Popup>("EmojiPopup")!.Placement = PlacementMode.Bottom;
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
        var list = f.Window.FindControl<ListBox>("EmojiGridList")!;
        var container = (Control)list.ContainerFromIndex(1)!;
        Assert.NotNull(container);
        var expected = f.Emoji.Candidates[1].Entry.Sequence;
        var point = container.TranslatePoint(new Point(25, 25), f.Window)!.Value;
        f.Window.MouseDown(point, MouseButton.Left);
        f.Window.MouseUp(point, MouseButton.Left);
        for (var i = 0; i < 100 && f.Window.IsEmojiInputBusy; i++)
            await Task.Delay(10);
        Assert.Equal(expected, Assert.Single(f.Input.Sent));
    }

    [AvaloniaFact]
    public async Task ClickingCategoryFiltersGridAndReturnsFocusToQuery()
    {
        using var f = new Fixture(style: EmojiCandidateStyle.Grid);
        await SearchFinished(f, "");
        f.Window.Height = 600;
        f.Window.FindControl<Popup>("EmojiPopup")!.Placement = PlacementMode.Bottom;
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
        var tabs = f.Window.FindControl<ListBox>("EmojiCategoriesList")!;
        var tab = (Control)tabs.ContainerFromIndex(3)!;
        var point = tab.TranslatePoint(new Point(15, 15), f.Window)!.Value;
        f.Window.MouseDown(point, MouseButton.Left);
        f.Window.MouseUp(point, MouseButton.Left);
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Input);
        Assert.Equal(3, tabs.SelectedIndex);
        Assert.Equal(3, f.Emoji.SelectedCategoryIndex);
        Assert.All(f.Emoji.Candidates, c => Assert.Equal("Animals & Nature", c.Entry.Group));
        Assert.True(f.Window.FindControl<TextBox>("QueryBox")!.IsFocused);
        Assert.Empty(f.Input.Sent);
    }

    [AvaloniaFact]
    public async Task SwitchingStylesWithNoQueryOpensBrowseAndRestoresList()
    {
        using var f = new Fixture();
        await SearchFinished(f, "");
        var strings = TranslationCatalog.Default.Resolve("zh-Hans");
        f.Emoji.ApplyConfiguration(strings, new AppSettings { EmojiCandidateStyle = EmojiCandidateStyle.Grid });
        await SearchFinished(f, "");
        Assert.Equal(1, f.Emoji.SelectedCategoryIndex);
        Assert.True(f.Emoji.Candidates.Count > 12);
        f.Emoji.ApplyConfiguration(strings, new AppSettings());
        await SearchFinished(f, "");
        Assert.False(f.Emoji.IsGrid);
        Assert.Equal(12, f.Emoji.Candidates.Count);
        Assert.True(f.Window.FindControl<ListBox>("EmojiCandidatesList")!.IsVisible);
        Assert.False(f.Window.FindControl<ListBox>("EmojiCategoriesList")!.IsVisible);
    }

    [AvaloniaFact]
    public async Task RefocusingVisiblePanelAndContinuousInputPreserveOriginalTarget()
    {
        using var f = new Fixture(keepOpen: true);
        await Ready(f, "dianzan");
        var original = f.Input.TargetToCapture;
        f.Input.TargetToCapture = null; // Searchbar itself now owns foreground focus.
        f.Window.ShowFromTray();
        await f.Window.InsertEmojiAsync();
        await f.Window.InsertEmojiAsync();
        Assert.Equal(new[] { original, original }, f.Input.Targets);
        Assert.True(f.Window.IsVisible);
    }

    [AvaloniaFact]
    public async Task ReopeningVisiblePanelFromAnotherAppUpdatesTheTarget()
    {
        using var f = new Fixture();
        await Ready(f, "dianzan");
        var otherTarget = new ExternalInputTarget(new IntPtr(84), 84, new IntPtr(85));
        f.Input.TargetToCapture = otherTarget;
        f.Window.ShowFromTray();
        await f.Window.InsertEmojiAsync();
        Assert.Equal(otherTarget, Assert.Single(f.Input.Targets));
    }

    [AvaloniaFact]
    public async Task NewSessionWithoutExternalTargetDoesNotReuseThePreviousApp()
    {
        using var f = new Fixture();
        f.Window.HideToTray();
        f.Input.TargetToCapture = null;
        f.Window.ShowFromTray();
        await Ready(f, "dianzan");
        await f.Window.InsertEmojiAsync();
        Assert.Null(Assert.Single(f.Input.Targets));
        Assert.True(f.Window.IsVisible);
        Assert.NotEmpty(f.Emoji.Status);
    }

    [AvaloniaFact]
    public async Task EnterInsertsSelectedSequenceAndClosesWithoutChangingClipboard()
    {
        using var f = new Fixture();
        await Ready(f, "dianzan");
        await f.Window.Clipboard!.SetTextAsync("clipboard sentinel");
        f.Window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
        for (var i = 0; i < 100 && f.Window.IsEmojiInputBusy; i++)
            await Task.Delay(10);
        Assert.Equal("👍", Assert.Single(f.Input.Sent));
        Assert.False(f.Window.IsVisible);
        Assert.Equal("clipboard sentinel", await f.Window.Clipboard!.TryGetTextAsync());
    }
    [AvaloniaFact]
    public async Task KeepingPanelPreservesQueryAndSelectionAndCopiesExplicitly()
    {
        using var f = new Fixture(true);
        await Ready(f, "heart");
        var sequence = f.Emoji.Selected!.Entry.Sequence;
        await f.Window.InsertEmojiAsync();
        Assert.True(f.Window.IsVisible);
        Assert.Equal("heart", f.Main.Query);
        Assert.Equal(sequence, f.Emoji.Selected!.Entry.Sequence);
        await f.Window.CopyEmojiAsync();
        Assert.Equal(sequence, await f.Window.Clipboard!.TryGetTextAsync());
        Assert.Equal(2, new EmojiHistoryStore(f.DirectoryPath).Snapshot().Single().Count);
    }
    [AvaloniaFact]
    public async Task VariantsConfirmWithoutInsertingAndEscapeReturns()
    {
        using var f = new Fixture();
        await Ready(f, "dianzan");
        f.Emoji.OpenVariants();
        Assert.True(f.Emoji.IsChoosingVariant);
        f.Emoji.SelectedIndex = 5;
        await f.Window.InsertEmojiAsync();
        Assert.False(f.Emoji.IsChoosingVariant);
        Assert.Empty(f.Input.Sent);
        Assert.Equal("👍🏿", f.Emoji.Selected!.Entry.Sequence);
        f.Emoji.OpenVariants();
        f.Window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, "");
        Assert.False(f.Emoji.IsChoosingVariant);
        Assert.True(f.Window.IsVisible);
    }
    [AvaloniaFact]
    public async Task ClickingCandidateUsesSameInputPath()
    {
        using var f = new Fixture();
        await Ready(f, "smile");
        // Headless renders popups inside the owner and clips raw mouse input to its client bounds.
        // Native Windows popups have their own surface; enlarge only this headless click host.
        f.Window.Height = 600;
        f.Window.FindControl<Popup>("EmojiPopup")!.Placement = PlacementMode.Bottom;
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
        var list = f.Window.FindControl<ListBox>("EmojiCandidatesList")!;
        var container = (Control)list.ContainerFromIndex(1)!;
        Assert.NotNull(container);
        var expected = f.Emoji.Candidates[1].Entry.Sequence;
        var point = container.TranslatePoint(new Point(80, container.Bounds.Height / 2), f.Window)!.Value;
        f.Window.MouseDown(point, MouseButton.Left);
        f.Window.MouseUp(point, MouseButton.Left);
        for (var i = 0; i < 100 && f.Input.Sent.Count == 0; i++)
            await Task.Delay(10);
        Assert.Equal(expected, Assert.Single(f.Input.Sent));
        for (var i = 0; i < 100 && f.Window.IsEmojiInputBusy; i++)
            await Task.Delay(10);
    }
    [AvaloniaFact]
    public async Task FailedInputPreservesCandidatesAndModeSwitchHidesPopup()
    {
        using var f = new Fixture();
        await Ready(f, "weixiao");
        f.Input.Succeed = false;
        await f.Window.InsertEmojiAsync();
        Assert.True(f.Window.IsVisible);
        Assert.Equal("weixiao", f.Main.Query);
        Assert.NotEmpty(f.Emoji.Status);
        f.Main.CycleMode();
        Assert.False(f.Main.IsEmojiPopupOpen);
        Assert.Empty(f.Emoji.Candidates);
    }
    [AvaloniaFact]
    public async Task FastQueriesCannotRestoreStaleResults()
    {
        using var f = new Fixture();
        f.Main.Query = "smile";
        f.Main.Query = "fire";
        await Ready(f, "dianzan");
        Assert.Equal("👍", f.Emoji.Candidates[0].Entry.Sequence);
        f.Main.OpenModeWheel();
        Assert.False(f.Main.IsEmojiPopupOpen);
        f.Main.CancelModeWheel();
        Assert.True(f.Main.IsEmojiPopupOpen);
        f.Window.HideToTray();
        Assert.False(f.Main.IsEmojiPopupOpen);
    }
    [Fact]
    public void NativeInputPreservesEveryUtf16CodeUnitAndIgnoresOtherShellChords()
    {
        const string text = "👩🏽‍💻❤️🇨🇳";
        var inputs = EmojiTextInputService.CreateUnicodeInputs(text);
        Assert.Equal(text.Length * 2, inputs.Length);
        for (var i = 0; i < text.Length; i++)
        {
            Assert.Equal(text[i], (char)inputs[i * 2].Keyboard.Scan);
            Assert.Equal(4u, inputs[i * 2].Keyboard.Flags);
            Assert.Equal(6u, inputs[i * 2 + 1].Keyboard.Flags);
        }
        Assert.True(EmojiHotkeyService.IsShellEmojiBinding(new(ShortcutModifiers.Win, ".")));
        Assert.False(EmojiHotkeyService.IsShellEmojiBinding(new(ShortcutModifiers.Win, "E")));
        Assert.False(EmojiHotkeyService.IsShellEmojiBinding(new(ShortcutModifiers.Win | ShortcutModifiers.Shift, ".")));
    }
}
