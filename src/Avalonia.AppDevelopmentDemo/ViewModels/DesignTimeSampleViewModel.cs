namespace Avalonia.AppDevelopmentDemo.ViewModels
{
    public sealed class DesignTimeSampleViewModel
    {
        public string Title { get; init; } = "设计器数据";

        public string Summary { get; init; } = "这段文字来自 Design.DataContext。";

        public static DesignTimeSampleViewModel CreateRuntime() => new()
        {
            Title = "运行时数据",
            Summary = "Design.DataContext 只服务预览器，不会覆盖运行时 DataContext。"
        };
    }
}
