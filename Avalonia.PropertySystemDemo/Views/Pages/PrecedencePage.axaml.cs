using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.PropertySystemDemo.ViewModels;

namespace Avalonia.PropertySystemDemo.Views.Pages
{
    public partial class PrecedencePage : UserControl
    {
        public PrecedencePage()
        {
            InitializeComponent();
            DataContext = new PrecedenceViewModel();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            Read();
        }

        private void OnReadClick(object? sender, RoutedEventArgs e) => Read();

        private void Read()
        {
            ReadoutA.Text = $"A 实际宽度：{ProbeA.Bounds.Width:F0}（样式给的 300）";
            ReadoutB.Text = $"B 实际宽度：{ProbeB.Bounds.Width:F0}（本地值 150 压过样式 300）";
            ReadoutC.Text = $"C 实际宽度：{ProbeC.Bounds.Width:F0}（跟随滑块）";
        }
    }
}
