using CrispySearchbar.Platform;
using Xunit;

namespace CrispySearchbar.Ui.Tests;

public class FullscreenWindowClassifierTests
{
    private static readonly FullscreenRect PrimaryMonitor = new(0, 0, 1920, 1080);

    [Fact]
    public void ExactMonitorCoverage_IsFullscreen()
    {
        Assert.True(FullscreenWindowClassifier.CoversMonitor(
            PrimaryMonitor,
            PrimaryMonitor));
    }

    [Fact]
    public void WindowExtendingBeyondMonitor_IsFullscreen()
    {
        Assert.True(FullscreenWindowClassifier.CoversMonitor(
            new FullscreenRect(-1, -1, 1922, 1082),
            PrimaryMonitor));
    }

    [Fact]
    public void PartialCoverage_IsNotFullscreen()
    {
        Assert.False(FullscreenWindowClassifier.CoversMonitor(
            new FullscreenRect(0, 0, 1920, 1040),
            PrimaryMonitor));
    }

    [Fact]
    public void SmallDwmBorderGap_IsAcceptedWithinTolerance()
    {
        Assert.True(FullscreenWindowClassifier.CoversMonitor(
            new FullscreenRect(2, 1, 1916, 1078),
            PrimaryMonitor));
    }

    [Fact]
    public void GapBeyondTolerance_IsNotFullscreen()
    {
        Assert.False(FullscreenWindowClassifier.CoversMonitor(
            new FullscreenRect(3, 0, 1917, 1080),
            PrimaryMonitor,
            tolerancePixels: 2));
    }

    [Fact]
    public void SecondaryMonitorCoordinates_AreComparedRelatively()
    {
        var secondaryMonitor = new FullscreenRect(-1920, 0, 1920, 1080);

        Assert.True(FullscreenWindowClassifier.CoversMonitor(
            new FullscreenRect(-1920, 0, 1920, 1080),
            secondaryMonitor));
        Assert.False(FullscreenWindowClassifier.CoversMonitor(
            new FullscreenRect(0, 0, 1920, 1080),
            secondaryMonitor));
    }
}
