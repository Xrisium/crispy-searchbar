using System.Runtime.InteropServices;
using Avalonia.Threading;
using CrispySearchbar.Core.Configuration;

namespace CrispySearchbar.Platform;

/// <summary>全局快捷键抽象，支持启动时注册与运行期原子重注册。</summary>
public interface IGlobalHotkeyService : IDisposable
{
    event Action? Pressed;

    bool IsRegistered { get; }

    ShortcutBinding? CurrentBinding { get; }

    /// <summary>启动消息线程并注册；失败返回 false（例如组合键已被占用）。</summary>
    bool TryStart(ShortcutBinding binding);

    /// <summary>运行期切换/注销新键位；失败时尽量保持旧键位并返回 false。</summary>
    bool TryApply(ShortcutBinding? binding);
}

/// <summary>创建当前平台可用的全局快捷键服务（非 Windows 时为 no-op）。</summary>
public static class GlobalHotkeyServiceFactory
{
    public static IGlobalHotkeyService Create()
        => OperatingSystem.IsWindows()
            ? new WindowsGlobalHotkeyService()
            : new NoopGlobalHotkeyService();
}

/// <summary>全局热键占用探测：临时注册成功后立即注销，不改变当前服务状态。</summary>
public interface IGlobalHotkeyProbe : IDisposable
{
    bool CanRegister(string canonicalShortcut);
}

public static class GlobalHotkeyProbeFactory
{
    public static IGlobalHotkeyProbe Create()
        => OperatingSystem.IsWindows()
            ? new WindowsGlobalHotkeyProbe()
            : new NoopGlobalHotkeyProbe();
}

/// <summary>Windows 实现：RegisterHotKey + 消息专用窗口接收 WM_HOTKEY。</summary>
public sealed class WindowsGlobalHotkeyService : IGlobalHotkeyService
{
    private const int HotkeyId = 0xC51A;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWin = 0x0008;
    private const uint ModNoRepeat = 0x4000;
    private const uint WmHotkey = 0x0312;
    private const uint WmQuit = 0x0012;
    private const uint WmApplyHotkey = 0x8001;
    private const int HwndMessage = -3;
    private const string WindowClassName = "CrispySearchbarHotkeyWindow";

    private readonly object _gate = new();
    private readonly ManualResetEventSlim _startCompleted = new(false);
    private readonly ManualResetEventSlim _applyCompleted = new(false);
    private readonly string _windowClassName =
        WindowClassName + Guid.NewGuid().ToString("N");
    private Thread? _thread;
    private uint _threadId;
    private IntPtr _windowHandle;
    private WndProcDelegate? _wndProc;
    private bool _started;
    private bool _disposed;
    private bool _isRegistered;
    private bool _operationSucceeded;
    private ShortcutBinding? _currentBinding;
    private ShortcutBinding? _requestedBinding;

    public event Action? Pressed;

    public bool IsRegistered
    {
        get
        {
            lock (_gate)
            {
                return _isRegistered;
            }
        }
    }

    public ShortcutBinding? CurrentBinding
    {
        get
        {
            lock (_gate)
            {
                return _currentBinding;
            }
        }
    }

    public bool TryStart(ShortcutBinding binding)
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        lock (_gate)
        {
            if (_disposed || _started)
            {
                return false;
            }

            _requestedBinding = binding;
            _started = true;
            _operationSucceeded = false;
            _thread = new Thread(RunMessageLoop)
            {
                IsBackground = true,
                Name = "CrispySearchbar.Hotkey",
            };
            _thread.SetApartmentState(ApartmentState.STA);
            _startCompleted.Reset();
            _thread.Start();
        }

        _startCompleted.Wait(TimeSpan.FromSeconds(3));
        lock (_gate)
        {
            return _operationSucceeded && _isRegistered;
        }
    }

    public bool TryApply(ShortcutBinding? binding)
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        bool needsStart;
        lock (_gate)
        {
            if (_disposed)
            {
                return false;
            }

            needsStart = !_started || _thread is not { IsAlive: true };
        }

        if (needsStart)
        {
            return binding is null || TryStart(binding);
        }

        IntPtr windowHandle;
        lock (_gate)
        {
            if (_disposed)
            {
                return false;
            }

            if (binding is not null
                && _isRegistered
                && _currentBinding is { } current
                && current == binding)
            {
                return true;
            }

            _requestedBinding = binding;
            windowHandle = _windowHandle;
        }

        if (windowHandle == IntPtr.Zero
            || !PostMessage(windowHandle, WmApplyHotkey, IntPtr.Zero, IntPtr.Zero))
        {
            return false;
        }

        _operationSucceeded = false;
        _applyCompleted.Reset();
        if (!_applyCompleted.Wait(TimeSpan.FromSeconds(3)))
        {
            return false;
        }

        lock (_gate)
        {
            return _operationSucceeded && _isRegistered && _currentBinding == binding;
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (!_started)
            {
                return;
            }
        }

        if (_thread is { IsAlive: true })
        {
            PostThreadMessage(_threadId, WmQuit, IntPtr.Zero, IntPtr.Zero);
            _thread.Join(TimeSpan.FromSeconds(1));
        }

        _wndProc = null;
    }

    private void RunMessageLoop()
    {
        _threadId = GetCurrentThreadId();
        _wndProc = WndProc;

        var windowClass = new WndClass
        {
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
            hInstance = GetModuleHandle(null),
            lpszClassName = _windowClassName,
        };

        RegisterClass(ref windowClass);

        var windowHandle = CreateWindowEx(
            0,
            _windowClassName,
            _windowClassName,
            0,
            0, 0, 0, 0,
            new IntPtr(HwndMessage),
            IntPtr.Zero,
            windowClass.hInstance,
            IntPtr.Zero);

        if (windowHandle == IntPtr.Zero)
        {
            lock (_gate)
            {
                _windowHandle = IntPtr.Zero;
                _isRegistered = false;
                _currentBinding = null;
            }

            UnregisterClass(_windowClassName, GetModuleHandle(null));
            _operationSucceeded = false;
            _startCompleted.Set();
            return;
        }

        lock (_gate)
        {
            _windowHandle = windowHandle;
            _requestedBinding = NormalizeRequestedBinding();
        }

        var startedOk = RegisterCurrentOnWindow(windowHandle);
        lock (_gate)
        {
            _operationSucceeded = startedOk;
            if (startedOk)
            {
                _isRegistered = true;
                _currentBinding = _requestedBinding;
            }
            else
            {
                _isRegistered = false;
                _currentBinding = null;
            }
        }

        _startCompleted.Set();

        var message = new NativeMessage();
        while (GetMessage(out message, IntPtr.Zero, 0, 0) > 0)
        {
            TranslateMessage(ref message);
            DispatchMessage(ref message);
        }

        lock (_gate)
        {
            var handle = _windowHandle;
            _windowHandle = IntPtr.Zero;
            if (handle != IntPtr.Zero && _isRegistered)
            {
                UnregisterHotKey(handle, HotkeyId);
            }

            if (handle != IntPtr.Zero)
            {
                DestroyWindow(handle);
                UnregisterClass(_windowClassName, GetModuleHandle(null));
            }

            _isRegistered = false;
            _currentBinding = null;
        }
    }

    private ShortcutBinding? NormalizeRequestedBinding()
    {
        var requested = _requestedBinding;
        return requested is null
            ? null
            : ShortcutParser.TryParse(requested.ToStorageString(), out var normalized)
                ? normalized
                : requested;
    }

    private bool RegisterCurrentOnWindow(IntPtr windowHandle)
    {
        ShortcutBinding? binding;
        lock (_gate)
        {
            binding = _requestedBinding;
        }

        return binding is null || RegisterSingle(windowHandle, binding);
    }

    private bool RegisterSingle(IntPtr windowHandle, ShortcutBinding binding)
    {
        if (windowHandle == IntPtr.Zero)
        {
            return false;
        }

        return RegisterHotKey(
            windowHandle,
            HotkeyId,
            ToWin32Modifiers(binding.Modifiers) | ModNoRepeat,
            ToVirtualKey(binding.Key));
    }

    private void ApplyRequestedBinding()
    {
        ShortcutBinding? requested;
        lock (_gate)
        {
            requested = _requestedBinding;
        }

        var succeeded = false;
        var handle = _windowHandle;
        if (handle != IntPtr.Zero)
        {
            var previous = _currentBinding;
            if (requested is null)
            {
                if (previous is not null && _isRegistered)
                {
                    UnregisterHotKey(handle, HotkeyId);
                }

                lock (_gate)
                {
                    _isRegistered = false;
                    _currentBinding = null;
                }

                succeeded = true;
            }
            else if (previous is not null && previous == requested && _isRegistered)
            {
                succeeded = true;
            }
            else
            {
                if (previous is not null && _isRegistered)
                {
                    UnregisterHotKey(handle, HotkeyId);
                    _isRegistered = false;
                }

                if (RegisterSingle(handle, requested))
                {
                    lock (_gate)
                    {
                        _isRegistered = true;
                        _currentBinding = requested;
                    }

                    succeeded = true;
                }
                else if (previous is not null)
                {
                    // 注册失败时恢复旧键位，避免服务进入“无热键”状态。
                    var restored = RegisterSingle(handle, previous);
                    lock (_gate)
                    {
                        _isRegistered = restored;
                        _currentBinding = restored ? previous : null;
                    }
                }
                else
                {
                    lock (_gate)
                    {
                        _isRegistered = false;
                        _currentBinding = null;
                    }
                }
            }
        }

        _operationSucceeded = succeeded;
        _applyCompleted.Set();
    }

    private IntPtr WndProc(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam)
    {
        if (message == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            Dispatcher.UIThread.Post(() => Pressed?.Invoke());
            return IntPtr.Zero;
        }

        if (message == WmApplyHotkey)
        {
            ApplyRequestedBinding();
            return IntPtr.Zero;
        }

        return DefWindowProc(hWnd, message, wParam, lParam);
    }

    private static uint ToWin32Modifiers(ShortcutModifiers modifiers)
    {
        var result = 0u;
        if (modifiers.HasFlag(ShortcutModifiers.Alt))
        {
            result |= ModAlt;
        }

        if (modifiers.HasFlag(ShortcutModifiers.Control))
        {
            result |= ModControl;
        }

        if (modifiers.HasFlag(ShortcutModifiers.Shift))
        {
            result |= ModShift;
        }

        if (modifiers.HasFlag(ShortcutModifiers.Win))
        {
            result |= ModWin;
        }

        return result;
    }

    private static uint ToVirtualKey(string key)
    {
        if (key.Length == 1 && char.IsAsciiLetterOrDigit(key[0]))
        {
            return char.ToUpperInvariant(key[0]);
        }

        if (key.Length is >= 2 and <= 3
            && key[0] == 'F'
            && int.TryParse(key.AsSpan(1), out var functionNumber)
            && functionNumber is >= 1 and <= 24)
        {
            return (uint)(0x70 + functionNumber - 1);
        }

        return key switch
        {
            "-" => 0xBD,
            "=" => 0xBB,
            "[" => 0xDB,
            "]" => 0xDD,
            "\\" => 0xDC,
            ";" => 0xBA,
            "'" => 0xDE,
            "," => 0xBC,
            "." => 0xBE,
            "/" => 0xBF,
            "`" => 0xC0,
            "XButton1" => 0x05,
            "XButton2" => 0x06,
            "Space" => 0x20,
            "Tab" => 0x09,
            "Esc" => 0x1B,
            "Enter" => 0x0D,
            "Up" => 0x26,
            "Down" => 0x28,
            "Left" => 0x25,
            "Right" => 0x27,
            "Home" => 0x24,
            "End" => 0x23,
            "PageUp" => 0x21,
            "PageDown" => 0x22,
            "Insert" => 0x2D,
            "Delete" => 0x2E,
            "Backspace" => 0x08,
            _ => 0,
        };
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WndClass
    {
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        [MarshalAs(UnmanagedType.LPWStr)] public string? lpszMenuName;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpszClassName;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMessage
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public NativePoint pt;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClass(ref WndClass lpWndClass);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "CreateWindowExW", SetLastError = true)]
    private static extern IntPtr CreateWindowEx(
        uint dwExStyle,
        string lpClassName,
        string lpWindowName,
        uint dwStyle,
        int x,
        int y,
        int nWidth,
        int nHeight,
        IntPtr hWndParent,
        IntPtr hMenu,
        IntPtr hInstance,
        IntPtr lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterClass(string lpClassName, IntPtr hInstance);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern int GetMessage(out NativeMessage lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TranslateMessage(ref NativeMessage lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage(ref NativeMessage lpMsg);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostThreadMessage(uint idThread, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
}

/// <summary>Windows 全局热键占用探针。</summary>
public sealed class WindowsGlobalHotkeyProbe : IGlobalHotkeyProbe
{
    public bool CanRegister(string canonicalShortcut)
    {
        if (!ShortcutParser.TryParse(canonicalShortcut, out var binding))
        {
            return false;
        }

        using var probeService = new WindowsGlobalHotkeyService();
        return probeService.TryStart(binding);
    }

    public void Dispose()
    {
    }
}

internal sealed class NoopGlobalHotkeyService : IGlobalHotkeyService
{
    private ShortcutBinding? _binding;

    public event Action? Pressed
    {
        add { }
        remove { }
    }

    public bool IsRegistered => true;

    public ShortcutBinding? CurrentBinding => _binding;

    public bool TryStart(ShortcutBinding binding)
    {
        _binding = binding;
        return true;
    }

    public bool TryApply(ShortcutBinding? binding)
    {
        _binding = binding;
        return true;
    }

    public void Dispose()
    {
    }
}

internal sealed class NoopGlobalHotkeyProbe : IGlobalHotkeyProbe
{
    public bool CanRegister(string canonicalShortcut)
        => ShortcutParser.TryParse(canonicalShortcut, out _);

    public void Dispose()
    {
    }
}