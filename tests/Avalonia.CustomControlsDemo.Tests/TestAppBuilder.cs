using Avalonia;
using Avalonia.CustomControlsDemo;
using Avalonia.CustomControlsDemo.Tests;
using Avalonia.Headless;
using Avalonia.Tests.Common;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Avalonia.CustomControlsDemo.Tests
{
    public static class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp() => HeadlessApp.Build<App>();
    }
}
