using System.Runtime.InteropServices;
using System.Text;

namespace CrispySearchbar.Platform;

public sealed record ExternalInputTarget(IntPtr Window, uint ProcessId);

public interface IEmojiTextInputService
{
    ExternalInputTarget? CaptureTarget();
    Task<bool> InsertAsync(ExternalInputTarget? target, string text, CancellationToken cancellationToken);
}

/// <summary>Unicode text injection deliberately does not touch the clipboard.</summary>
public sealed class EmojiTextInputService : IEmojiTextInputService
{
    public ExternalInputTarget? CaptureTarget()
    {
        if (!OperatingSystem.IsWindows())
            return null;
        var window = GetForegroundWindow();
        GetWindowThreadProcessId(window, out var process);
        var className = new StringBuilder(256);
        GetClassName(window, className, className.Capacity);
        return window == IntPtr.Zero || process == Environment.ProcessId || window == GetDesktopWindow()
            || window == GetShellWindow() || IsShellSurface(className.ToString()) ? null : new(window, process);
    }

    internal static bool IsShellSurface(string className) => className is "Progman" or "WorkerW"
        or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd" or "NotifyIconOverflowWindow" or "#32768";

    public async Task<bool> InsertAsync(ExternalInputTarget? target, string text, CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows() || target is null || !IsWindow(target.Window))
            return false;
        GetWindowThreadProcessId(target.Window, out var process);
        if (process != target.ProcessId || process == Environment.ProcessId)
            return false;
        // Wait for the executing key, mouse button and modifiers; never synthesize releases of physical keys.
        var deadline = Environment.TickCount64 + 1500;
        while (AnyInputKeyDown())
        {
            if (Environment.TickCount64 >= deadline)
                return false;
            await Task.Delay(10, cancellationToken);
        }
        if (IsIconic(target.Window) && !ShowWindowAsync(target.Window, 9))
            return false;
        if (!SetForegroundWindow(target.Window) && GetForegroundWindow() != target.Window)
            return false;
        deadline = Environment.TickCount64 + 400;
        while (GetForegroundWindow() != target.Window)
        {
            if (Environment.TickCount64 >= deadline)
                return false;
            await Task.Delay(10, cancellationToken);
        }
        cancellationToken.ThrowIfCancellationRequested();
        GetWindowThreadProcessId(target.Window, out process);
        if (process != target.ProcessId)
            return false;
        var inputs = CreateUnicodeInputs(text);
        if (GetForegroundWindow() != target.Window)
            return false;
        var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeInput>());
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

    private static bool AnyInputKeyDown()
    {
        for (var key = 1; key < 256; key++)
            if (key is not (0x14 or 0x90 or 0x91) && (GetAsyncKeyState(key) & 0x8000) != 0)
                return true;
        return false;
    }

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
    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, NativeInput[] inputs, int size);
}
