using System.Runtime.InteropServices;
using Avalonia.Threading;

namespace CrispySearchbar.Platform;

/// <summary>全局快捷键抽象，便于未来跨平台实现。</summary>
public interface IGlobalHotkeyService : IDisposable
{
    event Action? Pressed;

    bool IsRegistered { get; }

    void Start();
}

/// <summary>创建当前平台可用的全局快捷键服务（非 Windows 时为 no-op）。</summary>
public static class GlobalHotkeyServiceFactory
{
    public static IGlobalHotkeyService Create()
        => OperatingSystem.IsWindows()
            ? new WindowsGlobalHotkeyService()
            : new WindowsGlobalHotkeyService.NoopGlobalHotkeyService();
}

/// <summary>Windows 实现：用 RegisterHotKey 注册 Alt+Space，通过消息专用窗口接收 WM_HOTKEY。</summary>
public sealed class WindowsGlobalHotkeyService : IGlobalHotkeyService
{
    private const int HotkeyId = 0xC51A;
    private const uint ModAlt = 0x0001;
    private const uint ModNoRepeat = 0x4000;
    private const uint VkSpace = 0x20;
    private const uint WmHotkey = 0x0312;
    private const uint WmQuit = 0x0012;
    private const int HwndMessage = -3;
    private const string WindowClassName = "CrispySearchbarHotkeyWindow";

    private readonly object _gate = new();
    private Thread? _thread;
    private uint _threadId;
    private IntPtr _windowHandle;
    private WndProcDelegate? _wndProc;
    private bool _started;

    public event Action? Pressed;

    public bool IsRegistered { get; private set; }

    public void Start()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        lock (_gate)
        {
            if (_started)
            {
                return;
            }

            _started = true;
        }

        _thread = new Thread(RunMessageLoop)
        {
            IsBackground = true,
            Name = "CrispySearchbar.Hotkey",
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (!_started)
            {
                return;
            }

            _started = false;
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
            lpszClassName = WindowClassName,
        };

        RegisterClass(ref windowClass);

        _windowHandle = CreateWindowEx(
            0,
            WindowClassName,
            WindowClassName,
            0,
            0, 0, 0, 0,
            new IntPtr(HwndMessage),
            IntPtr.Zero,
            windowClass.hInstance,
            IntPtr.Zero);

        if (_windowHandle != IntPtr.Zero)
        {
            IsRegistered = RegisterHotKey(_windowHandle, HotkeyId, ModAlt | ModNoRepeat, VkSpace);
        }

        var message = new NativeMessage();
        while (GetMessage(out message, IntPtr.Zero, 0, 0) > 0)
        {
            TranslateMessage(ref message);
            DispatchMessage(ref message);
        }

        if (_windowHandle != IntPtr.Zero)
        {
            UnregisterHotKey(_windowHandle, HotkeyId);
            DestroyWindow(_windowHandle);
            _windowHandle = IntPtr.Zero;
        }
    }

    private IntPtr WndProc(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam)
    {
        if (message == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            Dispatcher.UIThread.Post(() => Pressed?.Invoke());
            return IntPtr.Zero;
        }

        return DefWindowProc(hWnd, message, wParam, lParam);
    }

    internal sealed class NoopGlobalHotkeyService : IGlobalHotkeyService
    {
        public event Action? Pressed
        {
            add { }
            remove { }
        }

        public bool IsRegistered => false;

        public void Start()
        {
        }

        public void Dispose()
        {
        }
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
    private static extern bool PostThreadMessage(uint idThread, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
}

