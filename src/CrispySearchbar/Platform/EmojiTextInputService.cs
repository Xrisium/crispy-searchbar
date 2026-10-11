using System.Runtime.InteropServices;
using System.Text;

namespace CrispySearchbar.Platform;

public sealed record ExternalInputTarget(IntPtr Window, uint ProcessId, IntPtr FocusedWindow = default);

public interface IEmojiTextInputService
{
    ExternalInputTarget? CaptureTarget();
    Task<bool> InsertAsync(ExternalInputTarget? target, string text, CancellationToken cancellationToken);
}

/// <summary>Unicode text injection deliberately does not touch the clipboard.</summary>
public sealed class EmojiTextInputService : IEmojiTextInputService
{
    private readonly IEmojiInputPlatform _platform;

    public EmojiTextInputService() : this(new WindowsEmojiInputPlatform()) { }
    internal EmojiTextInputService(IEmojiInputPlatform platform) => _platform = platform;

    public ExternalInputTarget? CaptureTarget() => _platform.CaptureTarget();

    internal static bool IsShellSurface(string className) => className is "Progman" or "WorkerW"
        or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd" or "NotifyIconOverflowWindow" or "#32768";

    public async Task<bool> InsertAsync(ExternalInputTarget? target, string text, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (target is null || !_platform.IsValidTarget(target))
            return false;
        // Wait for the executing key, mouse button and modifiers; never synthesize releases of physical keys.
        var deadline = Environment.TickCount64 + 1500;
        while (_platform.AnyInputKeyDown())
        {
            if (Environment.TickCount64 >= deadline)
                return false;
            await Task.Delay(10, cancellationToken);
        }
        if (!_platform.IsValidTarget(target))
            return false;
        // Cross-thread activation is asynchronous. Judge the resulting foreground window,
        // rather than returning early from SetForegroundWindow's immediate result.
        _platform.RequestActivation(target);
        deadline = Environment.TickCount64 + 400;
        while (!_platform.IsForeground(target))
        {
            if (Environment.TickCount64 >= deadline)
                return false;
            await Task.Delay(10, cancellationToken);
        }
        cancellationToken.ThrowIfCancellationRequested();
        if (!_platform.IsValidTarget(target) || !_platform.RestoreFocus(target))
            return false;
        var inputs = CreateUnicodeInputs(text);
        cancellationToken.ThrowIfCancellationRequested();
        if (!_platform.IsForeground(target) || _platform.AnyInputKeyDown())
            return false;
        var sent = _platform.Send(inputs);
        if (sent != inputs.Length)
            return false; // Do not retry a possibly partially submitted sequence.
        // Give the target's message loop an opportunity to consume text before reactivating our panel.
        await Task.Delay(80, cancellationToken);
        return true;
    }

    internal static NativeInput[] CreateUnicodeInputs(string text)
        => text.SelectMany(c => new[] { UnicodeKey(c, false), UnicodeKey(c, true) }).ToArray();

    private static NativeInput UnicodeKey(char c, bool up) => new()
    {
        Type = 1,
        Keyboard = new()
        {
            Scan = c,
            Flags = 0x0004u | (up ? 0x0002u : 0)
        }
    };

    // INPUT is 40 bytes on x64. MOUSEINPUT determines the union's size, even for keyboard input.
    [StructLayout(LayoutKind.Explicit, Size = 40)]
    internal struct NativeInput
    {
        [FieldOffset(0)] public uint Type;
        [FieldOffset(8)] public KeyboardInput Keyboard;
    }
    [StructLayout(LayoutKind.Sequential)]
    internal struct KeyboardInput
    {
        public ushort VirtualKey, Scan;
        public uint Flags, Time;
        public UIntPtr ExtraInfo;
    }
}

/// <summary>Native operations are isolated so focus-transfer failures can be tested without injecting keys.</summary>
internal interface IEmojiInputPlatform
{
    ExternalInputTarget? CaptureTarget();
    bool IsValidTarget(ExternalInputTarget target);
    bool AnyInputKeyDown();
    void RequestActivation(ExternalInputTarget target);
    bool IsForeground(ExternalInputTarget target);
    bool RestoreFocus(ExternalInputTarget target);
    uint Send(EmojiTextInputService.NativeInput[] inputs);
}

internal sealed class WindowsEmojiInputPlatform : IEmojiInputPlatform
{
    public ExternalInputTarget? CaptureTarget()
    {
        if (!OperatingSystem.IsWindows())
            return null;
        var window = GetForegroundWindow();
        var thread = GetWindowThreadProcessId(window, out var process);
        var className = new StringBuilder(256);
        GetClassName(window, className, className.Capacity);
        if (window == IntPtr.Zero || process == Environment.ProcessId || window == GetDesktopWindow()
            || window == GetShellWindow() || EmojiTextInputService.IsShellSurface(className.ToString()))
            return null;
        var info = new GuiThreadInfo { Size = (uint)Marshal.SizeOf<GuiThreadInfo>() };
        var focus = GetGUIThreadInfo(thread, ref info) ? info.Focus : IntPtr.Zero;
        return new(window, process, focus);
    }

    public bool IsValidTarget(ExternalInputTarget target)
    {
        if (!OperatingSystem.IsWindows() || !IsWindow(target.Window))
            return false;
        GetWindowThreadProcessId(target.Window, out var process);
        return process == target.ProcessId && process != Environment.ProcessId;
    }

    public bool AnyInputKeyDown()
    {
        for (var key = 1; key < 256; key++)
            if (key is not (0x14 or 0x90 or 0x91) && (GetAsyncKeyState(key) & 0x8000) != 0)
                return true;
        return false;
    }

    public void RequestActivation(ExternalInputTarget target)
    {
        if (IsIconic(target.Window))
            ShowWindowAsync(target.Window, 9);
        SetForegroundWindow(target.Window);
    }

    public bool IsForeground(ExternalInputTarget target) => GetForegroundWindow() == target.Window;

    public bool RestoreFocus(ExternalInputTarget target)
    {
        // Activating a top-level window does not guarantee its original editor regains focus.
        // Preserve the actual focused HWND from before Searchbar took keyboard focus.
        if (target.FocusedWindow == IntPtr.Zero)
            return true;
        if (!IsValidTarget(target) || !IsForeground(target)
            || !IsWindow(target.FocusedWindow)
            || (target.FocusedWindow != target.Window && !IsChild(target.Window, target.FocusedWindow)))
            return false;
        var thread = GetWindowThreadProcessId(target.FocusedWindow, out var process);
        if (process != target.ProcessId)
            return false;
        if (HasFocus(thread, target.FocusedWindow))
            return true;

        var currentThread = GetCurrentThreadId();
        if (!AttachThreadInput(currentThread, thread, true))
            return false;
        try
        {
            if (!IsForeground(target))
                return false;
            SetFocus(target.FocusedWindow);
        }
        finally
        {
            AttachThreadInput(currentThread, thread, false);
        }
        return IsForeground(target) && HasFocus(thread, target.FocusedWindow);
    }

    private static bool HasFocus(uint thread, IntPtr window)
    {
        var info = new GuiThreadInfo { Size = (uint)Marshal.SizeOf<GuiThreadInfo>() };
        return GetGUIThreadInfo(thread, ref info) && info.Focus == window;
    }

    public uint Send(EmojiTextInputService.NativeInput[] inputs)
        => SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<EmojiTextInputService.NativeInput>());

    [StructLayout(LayoutKind.Sequential)]
    private struct GuiThreadInfo
    {
        public uint Size, Flags;
        public IntPtr Active, Focus, Capture, MenuOwner, MoveSize, Caret;
        public int CaretLeft, CaretTop, CaretRight, CaretBottom;
    }

    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern IntPtr GetDesktopWindow();
    [DllImport("user32.dll")] private static extern IntPtr GetShellWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);
    [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool ShowWindowAsync(IntPtr hwnd, int command);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr hwnd, StringBuilder name, int capacity);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] private static extern bool IsChild(IntPtr parent, IntPtr child);
    [DllImport("user32.dll")] private static extern bool GetGUIThreadInfo(uint thread, ref GuiThreadInfo info);
    [DllImport("user32.dll")] private static extern bool AttachThreadInput(uint from, uint to, bool attach);
    [DllImport("user32.dll")] private static extern IntPtr SetFocus(IntPtr window);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, EmojiTextInputService.NativeInput[] inputs, int size);
}
