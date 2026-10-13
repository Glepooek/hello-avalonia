using Avalonia;
using Avalonia.Headless;
using Avalonia.Tests.Common;
using Avalonia.XamlDemo;
using Avalonia.XamlDemo.Tests;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Avalonia.XamlDemo.Tests
{
    public static class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp() => HeadlessApp.Build<App>();
    }
}
