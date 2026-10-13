using Avalonia;
using Avalonia.DataBindingDemo;
using Avalonia.DataBindingDemo.Tests;
using Avalonia.Headless;
using Avalonia.Tests.Common;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Avalonia.DataBindingDemo.Tests
{
    public static class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp() => HeadlessApp.Build<App>();
    }
}
