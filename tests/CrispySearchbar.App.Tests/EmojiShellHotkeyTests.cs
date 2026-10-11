using System.IO.Compression;
using CrispySearchbar.Core.Emoji;
using CrispySearchbar.Platform;
using SkiaSharp;
using Xunit;

namespace CrispySearchbar.Ui.Tests;

public sealed class EmojiShellHotkeyTests
{
    [Fact]
    public void ExactChordTriggersOnceAndSuppressesTheWholePeriodPress()
    {
        var keys = new EmojiShellHotkeyState();
        Assert.False(keys.Handle(0x5b, false, false, () => false).Suppress);
        var press = keys.Handle(0xbe, false, false, () => false);
        Assert.True(press.Trigger);
        Assert.True(press.Suppress);
        Assert.True(press.MaskWinMenu);
        Assert.False(keys.Handle(0xbe, false, false, () => false).Trigger);
        keys.Handle(0x5b, true, false, () => false);
        Assert.True(keys.Handle(0xbe, false, false, () => false).Suppress);
        Assert.True(keys.Handle(0xbe, true, false, () => false).Suppress);
        Assert.False(keys.Handle(0xbe, false, false, () => false).Suppress);
    }

    [Theory]
    [InlineData(0xa0)]
    [InlineData(0xa2)]
    [InlineData(0xa4)]
    public void ExtraModifiersAndOtherWinShortcutsPassThrough(int modifier)
    {
        var keys = new EmojiShellHotkeyState();
        keys.Handle(0x5c, false, false, () => false);
        Assert.False(keys.Handle(0x45, false, false, () => false).Suppress); // Win+E
        keys.Handle(modifier, false, false, () => false);
        Assert.False(keys.Handle(0xbe, false, false, () => false).Suppress);
        Assert.False(keys.Handle(0xbe, true, false, () => false).Suppress);
    }

    [Fact]
    public void FullscreenAndInjectedKeysPassThroughAndResetDropsOldState()
    {
        var keys = new EmojiShellHotkeyState();
        keys.Handle(0x5b, false, false, () => false);
        Assert.False(keys.Handle(0xbe, false, false, () => true).Suppress);
        Assert.False(keys.Handle(0xbe, false, false, () => false).Trigger); // no takeover mid-repeat
        keys.Handle(0xbe, true, false, () => false);
        Assert.False(keys.Handle(0xbe, false, true, () => false).Suppress);
        Assert.True(keys.Handle(0xbe, false, false, () => false).Trigger);
        keys.Reset();
        Assert.False(keys.Handle(0xbe, false, false, () => false).Trigger);
    }

    [Fact]
    public void EveryBundledPreviewDecodesWithTheNativeImageLibrary()
    {
        using var stream = EmojiResources.OpenImages();
        using var archive = new ZipArchive(stream);
        foreach (var entry in archive.Entries)
        {
            using var compressed = entry.Open();
            using var png = new MemoryStream();
            compressed.CopyTo(png);
            png.Position = 0;
            using var bitmap = SKBitmap.Decode(png);
            Assert.NotNull(bitmap);
            Assert.Equal(72, bitmap.Width);
            Assert.Equal(72, bitmap.Height);
        }
    }

    [Fact]
    public void DesktopAndTrayAreNotTextTargetsButExplorerWindowsRemainEligible()
    {
        Assert.True(EmojiTextInputService.IsShellSurface("WorkerW"));
        Assert.True(EmojiTextInputService.IsShellSurface("Shell_TrayWnd"));
        Assert.True(EmojiTextInputService.IsShellSurface("NotifyIconOverflowWindow"));
        Assert.False(EmojiTextInputService.IsShellSurface("CabinetWClass"));
        Assert.False(EmojiTextInputService.IsShellSurface("Notepad"));
    }
}
