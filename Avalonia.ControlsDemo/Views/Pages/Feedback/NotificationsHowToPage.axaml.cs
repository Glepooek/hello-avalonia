using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class NotificationsHowToPage : UserControl
    {
        public NotificationsHowToPage()
        {
            InitializeComponent();
            var model = new NotificationsHowToViewModel();
            DataContext = model;

            var count = 0;
            InfoButton.Click += (_, _) => model.Show($"第 {++count} 条信息", "info", 3000);
            SuccessButton.Click += (_, _) => model.Show($"第 {++count} 条：已完成", "success", 3000);
            StickyButton.Click += (_, _) => model.Show($"第 {++count} 条：出错了，需要手动关闭", "error", 0);
        }
    }
}
