using System.Diagnostics;

namespace CrispySearchbar;

/// <summary>调用系统默认应用打开 URL 或本地文件。</summary>
public static class BrowserLauncher
{
    public static void Open(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    public static void OpenFileWithDefaultApplication(string path)
    {
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
}
