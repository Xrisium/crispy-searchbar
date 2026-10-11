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
        public bool Succeed { get; set; } = true;
        public ExternalInputTarget? CaptureTarget() => new(new IntPtr(42), 42);
        public Task<bool> InsertAsync(ExternalInputTarget? target, string text, CancellationToken cancellationToken)
        {
            Sent.Add(text);
            return Task.FromResult(Succeed);
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
        public Fixture(bool keepOpen = false)
        {
            TestStyles.Ensure();
            var settings = new AppSettings { EmojiKeepOpenAfterInsert = keepOpen };
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
