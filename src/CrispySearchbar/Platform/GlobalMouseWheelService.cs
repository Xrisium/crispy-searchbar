using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia.Threading;

namespace CrispySearchbar.Platform;

/// <summary>
/// 全局鼠标滚轮拦截抽象：模式轮盘呼出期间由主窗口启用，
/// 吞掉全屏垂直滚轮事件并把增量转发给调用方。
/// </summary>
public interface IGlobalMouseWheelService : IDisposable
{
    /// <summary>一次被拦截的垂直滚轮事件；参数为有符号增量（正数 = 向上滚动）。</summary>
    event Action<int>? WheelDelta;

    /// <summary>低层鼠标钩子当前是否已安装。</summary>
    bool IsActive { get; }

    /// <summary>安装全局拦截并等待确认；失败返回 false（保留窗口内原有滚轮行为）。</summary>
    bool TryStart();

    /// <summary>移除全局拦截；未启用时安全无操作。</summary>
    void Stop();
}

/// <summary>创建当前平台可用的全局滚轮拦截服务（非 Windows 时为 no-op）。</summary>
public static class GlobalMouseWheelServiceFactory
{
    public static IGlobalMouseWheelService Create()
        => OperatingSystem.IsWindows()
            ? new WindowsGlobalMouseWheelService()
            : new NoopGlobalMouseWheelService();
}

/// <summary>
/// Windows 实现：WH_MOUSE_LL 低层鼠标钩子只在轮盘呼出期间生效。
/// 钩子安装在专用 STA 消息循环线程上，回调通过 Dispatcher 回到 UI 线程。
/// </summary>
public sealed class WindowsGlobalMouseWheelService : IGlobalMouseWheelService
{
    private const int WhMouseLl = 14;
    private const int HcAction = 0;
    private const uint WmMouseWheel = 0x020A;
    private const uint WmQuit = 0x0012;
    private const uint WmEnableHook = 0x8002;
    private const uint WmDisableHook = 0x8003;
    private const int OperationTimeoutMs = 500;

    private readonly object _gate = new();
    private readonly ManualResetEventSlim _startCompleted = new(false);
    private readonly ManualResetEventSlim _operationCompleted = new(false);

    private Thread? _thread;
    private uint _threadId;
    private IntPtr _hookHandle;
    private LowLevelMouseProc? _hookProc;
    private bool _requestedActive;
    private bool _isActive;
    private bool _operationSucceeded;
    private bool _disposed;

    public event Action<int>? WheelDelta;

    public bool IsActive
    {
        get
        {
            lock (_gate)
            {
                return _isActive;
            }
        }
    }

    public bool TryStart()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        lock (_gate)
        {
            if (_disposed)
            {
                return false;
            }

            if (_isActive)
            {
                return true;
            }

            _requestedActive = true;
        }

        if (!EnsureThreadStarted())
        {
            lock (_gate)
            {
                _requestedActive = false;
            }

            return false;
        }

        return RequestOperation(WmEnableHook);
    }

    public void Stop()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _requestedActive = false;
            if (!_isActive && _thread is not { IsAlive: true })
            {
                return;
            }
        }

        RequestOperation(WmDisableHook);
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
        }

        if (_thread is { IsAlive: true } && _threadId != 0)
        {
            PostThreadMessage(_threadId, WmQuit, IntPtr.Zero, IntPtr.Zero);
            _thread.Join(TimeSpan.FromSeconds(1));
        }
    }

    [SupportedOSPlatform("windows")]
    private bool EnsureThreadStarted()
    {
        lock (_gate)
        {
            if (_thread is { IsAlive: true })
            {
                return true;
            }

            if (_disposed)
            {
                return false;
            }

            _startCompleted.Reset();
            _threadId = 0;
            _thread = new Thread(RunMessageLoop)
            {
                IsBackground = true,
                Name = "CrispySearchbar.MouseWheelHook",
            };
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.Start();
        }

        return _startCompleted.Wait(TimeSpan.FromMilliseconds(OperationTimeoutMs));
    }

    private bool RequestOperation(uint message)
    {
        uint threadId;
        lock (_gate)
        {
            if (_thread is not { IsAlive: true } || _threadId == 0)
            {
                return false;
            }

            threadId = _threadId;
        }

        _operationSucceeded = false;
        _operationCompleted.Reset();
        if (!PostThreadMessage(threadId, message, IntPtr.Zero, IntPtr.Zero))
        {
            return false;
        }

        if (!_operationCompleted.Wait(TimeSpan.FromMilliseconds(OperationTimeoutMs)))
        {
            return false;
        }

        lock (_gate)
        {
            return _operationSucceeded;
        }
    }

    private void RunMessageLoop()
    {
        _threadId = GetCurrentThreadId();
        _startCompleted.Set();

        var message = new NativeMessage();
        while (GetMessage(out message, IntPtr.Zero, 0, 0) > 0)
        {
            if (message.message == WmEnableHook)
            {
                EnableHook();
            }
            else if (message.message == WmDisableHook)
            {
                DisableHook();
            }
        }

        DisableHook();
    }

    private void EnableHook()
    {
        lock (_gate)
        {
            if (_hookHandle == IntPtr.Zero && _requestedActive)
            {
                _hookProc = MouseHookCallback;
                var hookHandle = SetWindowsHookEx(
                    WhMouseLl,
                    _hookProc,
                    GetModuleHandle(null),
                    0);
                _hookHandle = hookHandle;
                _isActive = hookHandle != IntPtr.Zero;
                _operationSucceeded = hookHandle != IntPtr.Zero;
            }
            else
            {
                _operationSucceeded = _isActive;
            }
        }

        _operationCompleted.Set();
    }

    private void DisableHook()
    {
        IntPtr hookHandle;
        lock (_gate)
        {
            hookHandle = _hookHandle;
        }

        if (hookHandle != IntPtr.Zero)
        {
            UnhookWindowsHookEx(hookHandle);
        }

        lock (_gate)
        {
            _hookHandle = IntPtr.Zero;
            _isActive = false;
            _operationSucceeded = true;
            _hookProc = null;
        }

        _operationCompleted.Set();
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode == HcAction && wParam.ToInt32() == WmMouseWheel)
        {
            var info = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            var delta = unchecked((short)((info.mouseData >> 16) & 0xFFFF));
            if (delta != 0)
            {
                Dispatcher.UIThread.Post(() => WheelDelta?.Invoke(delta));
            }

            // 返回非零值吞掉事件：底层窗口不会收到该次垂直滚动。
            return new IntPtr(1);
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

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

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public NativePoint pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(
        int idHook,
        LowLevelMouseProc lpfn,
        IntPtr hMod,
        uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern int GetMessage(out NativeMessage lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostThreadMessage(uint idThread, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
}

/// <summary>非 Windows 环境的 no-op 实现：全局拦截不可用，主窗口继续使用窗口内滚轮处理。</summary>
internal sealed class NoopGlobalMouseWheelService : IGlobalMouseWheelService
{
    public event Action<int>? WheelDelta
    {
        add { }
        remove { }
    }

    public bool IsActive => false;

    public bool TryStart() => true;

    public void Stop()
    {
    }

    public void Dispose()
    {
    }
}
