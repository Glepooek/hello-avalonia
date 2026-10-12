using Avalonia.Headless;

namespace Avalonia.Tests.Common
{
    public static class HeadlessApp
    {
        // UseSkia + UseHeadlessDrawing=false is what makes CaptureRenderedFrame return real pixels;
        // the default headless drawing produces 1x1 bitmaps.
        public static AppBuilder Build<TApp>() where TApp : Application, new() => AppBuilder.Configure<TApp>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
    }
}
