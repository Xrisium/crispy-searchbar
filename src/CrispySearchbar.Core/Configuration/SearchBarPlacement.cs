namespace CrispySearchbar.Core.Configuration;

/// <summary>
/// 搜索框窗口的最终摆放结果：物理像素坐标，以及浮层（候选/模式轮盘）应向上还是向下弹出。
/// </summary>
public readonly record struct SearchBarPlacement(int X, int Y, bool OverlaysAbove)
{
    /// <summary>
    /// 由主显示器工作区、窗口尺寸（DIP）、缩放比例与偏移量计算窗口左上角像素坐标。
    /// 计算顺序：工作区中央为基点 → 偏移按缩放比例换算为物理像素 → 整个窗口夹回工作区内。
    /// 工作区比窗口还小时贴工作区左上角，避免 <see cref="Math.Clamp(int, int, int)"/> 上下限翻转。
    /// </summary>
    public static SearchBarPlacement Compute(
        int workAreaX,
        int workAreaY,
        int workAreaWidth,
        int workAreaHeight,
        double windowWidthDip,
        double windowHeightDip,
        double scaling,
        ScreenOffset offset)
    {
        var scale = scaling > 0 ? scaling : 1d;
        var windowWidth = (int)Math.Round(windowWidthDip * scale, MidpointRounding.AwayFromZero);
        var windowHeight = (int)Math.Round(windowHeightDip * scale, MidpointRounding.AwayFromZero);

        var centeredX = workAreaX + (workAreaWidth - windowWidth) / 2;
        var centeredY = workAreaY + (workAreaHeight - windowHeight) / 2;

        var offsetX = (int)Math.Round(offset.X * scale, MidpointRounding.AwayFromZero);
        var offsetY = (int)Math.Round(offset.Y * scale, MidpointRounding.AwayFromZero);

        var x = ClampAxis(centeredX + offsetX, workAreaX, workAreaWidth, windowWidth);
        var y = ClampAxis(centeredY + offsetY, workAreaY, workAreaHeight, windowHeight);

        var windowCenterY = y + windowHeight / 2;
        var workAreaCenterY = workAreaY + workAreaHeight / 2;
        return new SearchBarPlacement(x, y, windowCenterY > workAreaCenterY);
    }

    private static int ClampAxis(int value, int areaOrigin, int areaLength, int windowLength)
    {
        var maxOffset = areaLength - windowLength;
        if (maxOffset <= 0)
        {
            return areaOrigin;
        }

        return Math.Clamp(value, areaOrigin, areaOrigin + maxOffset);
    }
}
