using CrispySearchbar.Core.Configuration;
using Xunit;

namespace CrispySearchbar.Core.Tests;

/// <summary>
/// 搜索框定位纯函数：主显示器工作区中默认居中，偏移按缩放换算后再整体夹回工作区。
/// </summary>
public class SearchBarPlacementTests
{
    private const double WindowWidthDip = 660;
    private const double WindowHeightDip = 88;

    [Fact]
    public void Compute_WithoutOffset_CentersWindowInWorkArea()
    {
        var placement = SearchBarPlacement.Compute(
            workAreaX: 0,
            workAreaY: 0,
            workAreaWidth: 1920,
            workAreaHeight: 1040,
            windowWidthDip: WindowWidthDip,
            windowHeightDip: WindowHeightDip,
            scaling: 1d,
            offset: ScreenOffset.Default);

        Assert.Equal(630, placement.X);
        Assert.Equal(476, placement.Y);
        Assert.False(placement.OverlaysAbove);
    }

    [Theory]
    [InlineData(1.25)]
    [InlineData(1.5)]
    [InlineData(2)]
    public void Compute_ScaledDisplay_KeepsWindowCentered(double scaling)
    {
        var workAreaWidth = (int)(1920 * scaling);
        var workAreaHeight = (int)(1040 * scaling);
        var expectedWidth = (int)Math.Round(WindowWidthDip * scaling, MidpointRounding.AwayFromZero);
        var expectedHeight = (int)Math.Round(WindowHeightDip * scaling, MidpointRounding.AwayFromZero);

        var placement = SearchBarPlacement.Compute(
            0,
            0,
            workAreaWidth,
            workAreaHeight,
            WindowWidthDip,
            WindowHeightDip,
            scaling,
            ScreenOffset.Default);

        Assert.Equal((workAreaWidth - expectedWidth) / 2, placement.X);
        Assert.Equal((workAreaHeight - expectedHeight) / 2, placement.Y);
    }

    [Fact]
    public void Compute_OffsetIsScaledToPhysicalPixels()
    {
        var placement = SearchBarPlacement.Compute(
            0,
            0,
            2400,
            1300,
            WindowWidthDip,
            WindowHeightDip,
            scaling: 1.25,
            offset: new ScreenOffset(100, -40));

        // 居中基点：(2400-825)/2 = 787、(1300-110)/2 = 595；偏移 100/40 DIP × 1.25 = 125/50 像素。
        Assert.Equal(912, placement.X);
        Assert.Equal(545, placement.Y);
    }

    [Theory]
    [InlineData(5000, 5000)]
    [InlineData(-5000, -5000)]
    public void Compute_OutOfScreenOffset_ClampsWindowInsideWorkArea(int offsetX, int offsetY)
    {
        var placement = SearchBarPlacement.Compute(
            0,
            0,
            1920,
            1040,
            WindowWidthDip,
            WindowHeightDip,
            scaling: 1d,
            offset: new ScreenOffset(offsetX, offsetY));

        Assert.InRange(placement.X, 0, 1920 - 660);
        Assert.InRange(placement.Y, 0, 1040 - 88);
        Assert.Equal(offsetX > 0 ? 1920 - 660 : 0, placement.X);
        Assert.Equal(offsetY > 0 ? 1040 - 88 : 0, placement.Y);
    }

    [Fact]
    public void Compute_WorkAreaSmallerThanWindow_PinsToWorkAreaOrigin()
    {
        var placement = SearchBarPlacement.Compute(
            workAreaX: 0,
            workAreaY: 0,
            workAreaWidth: 400,
            workAreaHeight: 60,
            windowWidthDip: WindowWidthDip,
            windowHeightDip: WindowHeightDip,
            scaling: 1d,
            offset: new ScreenOffset(-120, 240));

        Assert.Equal(0, placement.X);
        Assert.Equal(0, placement.Y);
    }

    [Fact]
    public void Compute_NonZeroWorkAreaOrigin_IsRespected()
    {
        var placement = SearchBarPlacement.Compute(
            workAreaX: -1920,
            workAreaY: 100,
            workAreaWidth: 1920,
            workAreaHeight: 1040,
            windowWidthDip: WindowWidthDip,
            windowHeightDip: WindowHeightDip,
            scaling: 1d,
            offset: ScreenOffset.Default);

        Assert.Equal(-1920 + 630, placement.X);
        Assert.Equal(100 + 476, placement.Y);
    }

    [Fact]
    public void Compute_OverlaysAbove_TracksLowerHalfOfWorkArea()
    {
        var above = SearchBarPlacement.Compute(
            0,
            0,
            1920,
            1040,
            WindowWidthDip,
            WindowHeightDip,
            scaling: 1d,
            offset: new ScreenOffset(0, 400));
        var below = SearchBarPlacement.Compute(
            0,
            0,
            1920,
            1040,
            WindowWidthDip,
            WindowHeightDip,
            scaling: 1d,
            offset: new ScreenOffset(0, -400));

        Assert.True(above.OverlaysAbove);
        Assert.False(below.OverlaysAbove);
    }

    [Fact]
    public void Default_IsCenteredAndNotClamped()
    {
        Assert.True(ScreenOffset.Default.IsDefault);
        Assert.Equal(new ScreenOffset(0, 0), ScreenOffset.Default);
        Assert.False(new ScreenOffset(1, 0).IsDefault);
    }
}
