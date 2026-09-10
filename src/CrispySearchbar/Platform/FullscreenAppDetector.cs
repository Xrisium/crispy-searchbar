using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace CrispySearchbar.Platform;

/// <summary>检测当前前台应用是否全屏。</summary>
public interface IFullscreenAppDetector
{
    bool IsFullscreenAppActive();
}

/// <summary>创建当前平台可用的全屏应用检测器（非 Windows 时始终返回 false）。</summary>
public static class FullscreenAppDetectorFactory
{
    public static IFullscreenAppDetector Create()
        => OperatingSystem.IsWindows()
            ? new WindowsFullscreenAppDetector()
            : new NoopFullscreenAppDetector();
}

/// <summary>与 Win32 无关的窗口/显示器覆盖判断，便于独立测试。</summary>
public readonly record struct FullscreenRect(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;

    public int Bottom => Y + Height;
}

public static class FullscreenWindowClassifier
{
    public const int DefaultTolerancePixels = 2;

    /// <summary>窗口边界是否完整覆盖显示器边界；允许少量 DWM 边框误差。</summary>
    public static bool CoversMonitor(
        FullscreenRect window,
        FullscreenRect monitor,
        int tolerancePixels = DefaultTolerancePixels)
    {
        if (window.Width <= 0 || window.Height <= 0)
        {
            return false;
        }

        var tolerance = Math.Max(0, tolerancePixels);
        return window.X <= monitor.X + tolerance
            && window.Y <= monitor.Y + tolerance
            && window.Right >= monitor.Right - tolerance
            && window.Bottom >= monitor.Bottom - tolerance;
    }
}

/// <summary>
/// Windows 实现：以前台窗口的 DWM 可见边界与窗口所在显示器的完整边界比较。
/// 检测失败时返回 false，避免误伤正常的快捷键呼出。
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsFullscreenAppDetector : IFullscreenAppDetector
{
    private const uint MonitorDefaultToNearest = 2;
    private const int DwmwaExtendedFrameBounds = 9;

    private static readonly string[] ShellWindowClasses =
    {
        "Progman",
        "WorkerW",
        "Shell_TrayWnd",
        "Shell_SecondaryTrayWnd",
    };

    public bool IsFullscreenAppActive()
    {
        var foregroundWindow = GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero
            || !IsWindowVisible(foregroundWindow)
            || IsIconic(foregroundWindow))
        {
            return false;
        }

        GetWindowThreadProcessId(foregroundWindow, out var processId);
        if (processId == (uint)Environment.ProcessId
            || IsShellWindow(foregroundWindow))
        {
            return false;
        }

        if (!TryGetVisibleBounds(foregroundWindow, out var windowBounds))
        {
            return false;
        }

        var monitor = MonitorFromWindow(foregroundWindow, MonitorDefaultToNearest);
        var monitorInfo = new MonitorInfo
        {
            Size = Marshal.SizeOf<MonitorInfo>(),
        };
        if (monitor == IntPtr.Zero
            || !GetMonitorInfo(monitor, ref monitorInfo))
        {
            return false;
        }

        return FullscreenWindowClassifier.CoversMonitor(
            ToFullscreenRect(windowBounds),
            ToFullscreenRect(monitorInfo.Monitor));
    }

    private static bool IsShellWindow(IntPtr window)
    {
        var className = new char[128];
        var length = GetClassName(window, className, className.Length);
        if (length <= 0)
        {
            return false;
        }

        var value = new string(className, 0, length);
        return ShellWindowClasses.Any(candidate => string.Equals(
            candidate,
            value,
            StringComparison.Ordinal));
    }

    private static bool TryGetVisibleBounds(IntPtr window, out NativeRect bounds)
    {
        var result = DwmGetWindowAttribute(
            window,
            DwmwaExtendedFrameBounds,
            out bounds,
            Marshal.SizeOf<NativeRect>());
        if (result == 0)
        {
            return true;
        }

        return GetWindowRect(window, out bounds);
    }

    private static FullscreenRect ToFullscreenRect(NativeRect rect)
        => new(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect WorkArea;
        public uint Flags;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, char[] className, int maxCount);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out NativeRect rect);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hWnd, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(
        IntPtr hWnd,
        int attribute,
        out NativeRect value,
        int valueSize);
}

internal sealed class NoopFullscreenAppDetector : IFullscreenAppDetector
{
    public bool IsFullscreenAppActive() => false;
}
