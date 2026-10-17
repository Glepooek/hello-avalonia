using Avalonia.Headless;
using Avalonia.Tests.Common;
using Xunit;

[assembly: AvaloniaTestApplication(typeof(Avalonia.MusicStore.Tests.TestAppBuilder))]
// Album keeps its cache in "./Cache" relative to the process working directory and the tests point that at a
// temp folder; the working directory is process-wide, so the tests in this assembly run one at a time.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Avalonia.MusicStore.Tests
{
    public static class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp() => HeadlessApp.Build<App>();
    }
}
