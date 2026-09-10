namespace CrispySearchbar.Core.Configuration;

/// <summary>
/// 搜索框相对主显示器工作区中央的偏移量，单位为 DIP（与窗口尺寸同一坐标系）。
/// X 为正表示向右、Y 为正表示向下；(0, 0) 即默认的屏幕居中位置。
/// </summary>
public readonly record struct ScreenOffset(int X, int Y)
{
    /// <summary>默认位置：主显示器工作区中央。</summary>
    public static ScreenOffset Default => new(0, 0);

    /// <summary>是否等同于默认居中位置。</summary>
    public bool IsDefault => X == 0 && Y == 0;
}
