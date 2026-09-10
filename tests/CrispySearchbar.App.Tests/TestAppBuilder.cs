using Avalonia;
using Avalonia.Headless;
using CrispySearchbar.Ui.Tests;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace CrispySearchbar.Ui.Tests;

/// <summary>Headless Avalonia 宿主：让交互测试可以驱动真实的窗口与路由事件。</summary>
public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<Application>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = true });
}
