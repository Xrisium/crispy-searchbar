using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace CrispySearchbar.Platform;

/// <summary>当前用户级开机自启注册状态。</summary>
public interface IStartupRegistrationService
{
    /// <summary>创建或删除当前用户的启动项；成功返回 true。</summary>
    bool TrySetEnabled(bool enabled);

    /// <summary>
    /// 启动时的自愈检查：启用状态且注册项指向旧的 exe 路径（便携版被移动过）时改写为当前路径；
    /// 禁用状态下删除注册项。成功返回 true。
    /// </summary>
    bool TryEnsureCurrentPath(bool enabled);
}

/// <summary>创建当前平台可用的自启注册服务（非 Windows 时为 no-op）。</summary>
public static class StartupRegistrationServiceFactory
{
    public static IStartupRegistrationService Create()
        => OperatingSystem.IsWindows()
            ? new WindowsStartupRegistrationService()
            : new NoopStartupRegistrationService();
}

/// <summary>构造 Windows 启动项命令行。</summary>
public static class StartupRegistrationCommand
{
    public const string AutostartArgument = "--autostart";

    /// <summary>
    /// 为普通 exe 或通过 dotnet 宿主启动的 dll 生成带引号的绝对命令行。
    /// 路径无法规范化时返回 null。
    /// </summary>
    public static string? Build(string executablePath, string? assemblyPath = null)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return null;
        }

        try
        {
            var executable = Path.GetFullPath(executablePath);
            if (IsDotNetHost(executable) && !string.IsNullOrWhiteSpace(assemblyPath))
            {
                var assembly = Path.GetFullPath(assemblyPath);
                return $"{Quote(executable)} {Quote(assembly)} {AutostartArgument}";
            }

            return $"{Quote(executable)} {AutostartArgument}";
        }
        catch (Exception ex) when (
            ex is ArgumentException
                or NotSupportedException
                or PathTooLongException)
        {
            return null;
        }
    }

    /// <summary>为当前进程生成启动项命令行。</summary>
    public static string? BuildCurrent()
        => Build(
            Environment.ProcessPath ?? string.Empty,
            GetEntryAssemblyPath());

    /// <summary>
    /// 通过 dotnet 宿主启动（dotnet app.dll）时返回入口程序集路径，普通 exe 启动返回 null。
    /// 不使用 <c>Assembly.Location</c>：单文件发布时它恒为空字符串，改从命令行首个参数取。
    /// </summary>
    private static string? GetEntryAssemblyPath()
    {
        var executable = Environment.ProcessPath ?? string.Empty;
        if (!IsDotNetHost(executable))
        {
            return null;
        }

        var entry = Environment.GetCommandLineArgs().FirstOrDefault();
        return string.IsNullOrWhiteSpace(entry) ? null : entry;
    }

    private static bool IsDotNetHost(string executablePath)
        => string.Equals(
            Path.GetFileNameWithoutExtension(executablePath),
            "dotnet",
            StringComparison.OrdinalIgnoreCase);

    private static string Quote(string path) => "\"" + path + "\"";
}

/// <summary>Windows 实现：读写 HKCU 的当前用户 Run 启动项。</summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsStartupRegistrationService : IStartupRegistrationService
{
    public const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public const string ValueName = "Crispy Searchbar";

    public bool TrySetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            if (key is null)
            {
                return !enabled;
            }

            if (enabled)
            {
                var command = StartupRegistrationCommand.BuildCurrent();
                if (command is null)
                {
                    return false;
                }

                key.SetValue(ValueName, command, RegistryValueKind.String);
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }

            return true;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning(
                "无法更新开机自启设置：{0}",
                ex.Message);
            return false;
        }
    }

    public bool TryEnsureCurrentPath(bool enabled)
    {
        // 禁用时仍需清理注册项（可能是旧版本或旧路径留下的），保持与设置一致。
        if (!enabled)
        {
            return TrySetEnabled(false);
        }

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            if (key is null)
            {
                return false;
            }

            var command = StartupRegistrationCommand.BuildCurrent();
            if (command is null)
            {
                return false;
            }

            if (string.Equals(
                    key.GetValue(ValueName) as string,
                    command,
                    StringComparison.Ordinal))
            {
                return true;
            }

            key.SetValue(ValueName, command, RegistryValueKind.String);
            return true;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("无法自愈开机自启路径：{0}", ex.Message);
            return false;
        }
    }
}

internal sealed class NoopStartupRegistrationService : IStartupRegistrationService
{
    public bool TrySetEnabled(bool enabled) => true;

    public bool TryEnsureCurrentPath(bool enabled) => true;
}
