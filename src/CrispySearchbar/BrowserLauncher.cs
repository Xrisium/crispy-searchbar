using System.Diagnostics;

namespace CrispySearchbar;

/// <summary>在用户默认浏览器中打开 URL。</summary>
public static class BrowserLauncher
{
    public static void Open(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
