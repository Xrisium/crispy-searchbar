using System.Runtime.InteropServices;
using Avalonia.Threading;
using CrispySearchbar.Core.Configuration;

namespace CrispySearchbar.Platform;

/// <summary>Win+. needs a narrow shell-shortcut hook. Other bindings use RegisterHotKey.</summary>
public sealed class EmojiHotkeyService : IDisposable
{
    private readonly IGlobalHotkeyService _ordinary = GlobalHotkeyServiceFactory.Create();
    private readonly Func<bool> _shouldSkip;
    private Thread? _thread;
    private uint _threadId;
    private IntPtr _hook;
    private HookProc? _callback;
    private readonly ManualResetEventSlim _ready = new(false);
    private readonly EmojiShellHotkeyState _keys = new();
    private volatile bool _enabled;
    public EmojiHotkeyService(Func<bool> shouldSkip)
    {
        _shouldSkip = shouldSkip;
        _ordinary.Pressed += OnOrdinaryPressed;
    }
    public event Action? Pressed;
    public bool IsRegistered => _hook != IntPtr.Zero || _ordinary.IsRegistered;
    public ShortcutBinding? CurrentBinding
    {
        get; private set;
    }
    public static bool IsShellEmojiBinding(ShortcutBinding binding)
        => binding.Modifiers == ShortcutModifiers.Win && binding.Key == ".";

    public static bool CanInstallShellHook()
    {
        if (!OperatingSystem.IsWindows())
            return false;
        HookProc callback = (code, message, pointer) => CallNextHookEx(IntPtr.Zero, code, message, pointer);
        var handle = SetWindowsHookEx(13, callback, GetModuleHandle(null), 0);
        if (handle != IntPtr.Zero)
            UnhookWindowsHookEx(handle);
        GC.KeepAlive(callback);
        return handle != IntPtr.Zero;
    }

    public bool TryApply(ShortcutBinding? binding)
    {
        if (CurrentBinding == binding && (binding is null || IsRegistered))
            return true;
        StopHook();
        _ordinary.TryApply(null);
        CurrentBinding = binding;
        if (binding is null)
            return true;
        if (!IsShellEmojiBinding(binding))
            return _ordinary.TryApply(binding);
        if (!OperatingSystem.IsWindows())
            return false;
        _ready.Reset();
        _enabled = true;
        _thread = new Thread(Run) { IsBackground = true, Name = "CrispySearchbar.EmojiHotkey" };
        _thread.Start();
        return _ready.Wait(1000) && _hook != IntPtr.Zero;
    }
    private void OnOrdinaryPressed()
    {
        if (!_shouldSkip())
            Pressed?.Invoke();
    }
    private void Run()
    {
        _threadId = GetCurrentThreadId();
        _callback = Callback;
        _hook = SetWindowsHookEx(13, _callback, GetModuleHandle(null), 0);
        // Ensure a message queue exists before publishing readiness.
        PeekMessage(out _, IntPtr.Zero, 0, 0, 0);
        _ready.Set();
        if (_hook == IntPtr.Zero)
            return;
        try
        {
            while (GetMessage(out var message, IntPtr.Zero, 0, 0) > 0)
            {
                TranslateMessage(ref message);
                DispatchMessage(ref message);
            }
        }
        finally { UnhookWindowsHookEx(_hook); _hook = IntPtr.Zero; }
    }
    private IntPtr Callback(int code, IntPtr message, IntPtr pointer)
    {
        if (code < 0 || !_enabled)
            return CallNextHookEx(_hook, code, message, pointer);
        var info = Marshal.PtrToStructure<KeyboardHook>(pointer);
        var up = message.ToInt64() is 0x0101 or 0x0105;
        var decision = _keys.Handle((int)info.Key, up, (info.Flags & 0x10) != 0, _shouldSkip);
        if (decision.MaskWinMenu)
        {
            // Unassigned vkE8 marks the Win chord as used without releasing a physical Win key.
            keybd_event(0xe8, 0, 0, UIntPtr.Zero);
            keybd_event(0xe8, 0, 2, UIntPtr.Zero);
        }
        if (decision.Trigger)
            Dispatcher.UIThread.Post(() => { if (_enabled) Pressed?.Invoke(); });
        return decision.Suppress ? new IntPtr(1) : CallNextHookEx(_hook, code, message, pointer);
    }
    private void StopHook()
    {
        _enabled = false;
        if (_thread is { IsAlive: true })
        {
            PostThreadMessage(_threadId, 0x0012, UIntPtr.Zero, IntPtr.Zero);
            _thread.Join(1000);
        }
        _thread = null;
        _threadId = 0;
        _keys.Reset();
    }
    public void Dispose()
    {
        StopHook();
        _ordinary.Dispose();
        _ready.Dispose();
    }
    private delegate IntPtr HookProc(int code, IntPtr message, IntPtr pointer);
    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardHook
    {
        public uint Key, Scan, Flags, Time; public UIntPtr Extra;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMessage
    {
        public IntPtr Window; public uint Message; public UIntPtr WParam; public IntPtr LParam; public uint Time; public int X, Y; public uint Private;
    }
    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetWindowsHookEx(int type, HookProc callback, IntPtr module, uint thread);
    [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr pointer);
    [DllImport("user32.dll")] private static extern int GetMessage(out NativeMessage message, IntPtr window, uint min, uint max);
    [DllImport("user32.dll")] private static extern bool PeekMessage(out NativeMessage message, IntPtr window, uint min, uint max, uint remove);
    [DllImport("user32.dll")] private static extern bool TranslateMessage(ref NativeMessage message);
    [DllImport("user32.dll")] private static extern IntPtr DispatchMessage(ref NativeMessage message);
    [DllImport("user32.dll")] private static extern bool PostThreadMessage(uint thread, uint message, UIntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr GetModuleHandle(string? name);
}
