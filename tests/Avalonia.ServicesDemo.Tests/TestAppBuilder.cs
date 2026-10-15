using Avalonia;
using Avalonia.Headless;
using Avalonia.ServicesDemo;
using Avalonia.ServicesDemo.Tests;
using Avalonia.Tests.Common;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Avalonia.ServicesDemo.Tests
{
    public static class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp() => HeadlessApp.Build<App>();
    }
}
