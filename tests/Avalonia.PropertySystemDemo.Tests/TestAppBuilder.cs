using Avalonia;
using Avalonia.Headless;
using Avalonia.PropertySystemDemo;
using Avalonia.PropertySystemDemo.Tests;
using Avalonia.Tests.Common;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Avalonia.PropertySystemDemo.Tests
{
    public static class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp() => HeadlessApp.Build<App>();
    }
}
