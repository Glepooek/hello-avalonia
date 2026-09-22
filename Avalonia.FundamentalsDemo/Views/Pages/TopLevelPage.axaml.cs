using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.FundamentalsDemo.Views.Pages
{
    public partial class TopLevelPage : UserControl
    {
        public TopLevelPage()
        {
            InitializeComponent();
        }

        // The TopLevel only exists once the control is attached, so the first
        // read happens here rather than in the constructor.
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            Describe();
        }

        private void OnRefreshClick(object? sender, RoutedEventArgs e) => Describe();

        private void Describe()
        {
            var top = TopLevel.GetTopLevel(this);
            if (top is null)
            {
                TopLevelTypeText.Text = "尚未附加到任何 TopLevel";
                return;
            }

            TopLevelTypeText.Text = $"TopLevel 实际类型：{top.GetType().Name}";
            ScalingText.Text = $"渲染缩放：{top.RenderScaling:F2}（客户区 {top.ClientSize.Width:F0}×{top.ClientSize.Height:F0}）";
            ScreenText.Text = $"检测到显示器：{top.Screens?.ScreenCount.ToString() ?? "不可用"} 台";
            ServiceText.Text = $"剪贴板：{Availability(top.Clipboard is not null)}；"
                + $"存储服务：{Availability(top.StorageProvider is not null)}；"
                + $"输入法面板：{Availability(top.InputPane is not null)}";
        }

        private static string Availability(bool available) => available ? "可用" : "本平台不提供";
    }
}
