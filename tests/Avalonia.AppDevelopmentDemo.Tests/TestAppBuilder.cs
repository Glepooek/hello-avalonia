using Avalonia;
using Avalonia.AppDevelopmentDemo;
using Avalonia.AppDevelopmentDemo.Tests;
using Avalonia.Headless;
using Avalonia.Tests.Common;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Avalonia.AppDevelopmentDemo.Tests
{
    public static class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp() => HeadlessApp.Build<App>();
    }
}
