using Avalonia;
using Avalonia.Headless;
using CrispySearchbar.Ui.Tests;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace CrispySearchbar.Ui.Tests;

/// <summary>
/// Headless Avalonia 宿主：让交互测试可以驱动真实的窗口与路由事件。
/// 使用真实的 <see cref="global::CrispySearchbar.App"/>，使测试窗口与应用一样拿到
/// App.axaml 里的语义色资源与共享样式，从而可以断言解析后的样式结果。
/// </summary>
public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<global::CrispySearchbar.App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = true });
}
