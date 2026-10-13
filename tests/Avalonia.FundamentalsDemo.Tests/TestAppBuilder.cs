using Avalonia;
using Avalonia.FundamentalsDemo;
using Avalonia.FundamentalsDemo.Tests;
using Avalonia.Headless;
using Avalonia.Tests.Common;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Avalonia.FundamentalsDemo.Tests
{
    public static class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp() => HeadlessApp.Build<App>();
    }
}
