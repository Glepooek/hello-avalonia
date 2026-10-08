using Avalonia;
using Avalonia.Headless;
using Avalonia.TestingDemo;
using Avalonia.TestingDemo.Tests;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Avalonia.TestingDemo.Tests
{
    public static class TestAppBuilder
    {
        // UseSkia + UseHeadlessDrawing=false is what makes CaptureRenderedFrame return real pixels;
        // the default headless drawing produces 1x1 bitmaps.
        public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
    }
}
