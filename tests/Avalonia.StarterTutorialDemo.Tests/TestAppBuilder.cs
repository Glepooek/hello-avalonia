using Avalonia;
using Avalonia.Headless;
using Avalonia.StarterTutorialDemo;
using Avalonia.StarterTutorialDemo.Tests;
using Avalonia.Tests.Common;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Avalonia.StarterTutorialDemo.Tests
{
    public static class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp() => HeadlessApp.Build<App>();
    }
}
