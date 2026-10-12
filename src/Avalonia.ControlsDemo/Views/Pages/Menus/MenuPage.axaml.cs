using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class MenuPage : UserControl
    {
        public MenuPage()
        {
            InitializeComponent();

            // Click bubbles up from every MenuItem, so one handler on the page is enough.
            AddHandler(MenuItem.ClickEvent, (_, e) =>
            {
                if (e.Source is MenuItem item)
                {
                    LastText.Text = $"最近点击：{item.Header}";
                    ShowState();
                }
            });
            ShowState();
        }

        private void ShowState()
        {
            var align = LeftItem.IsChecked ? "左" : CenterItem.IsChecked ? "中" : RightItem.IsChecked ? "右" : "（无）";
            StateText.Text = $"粗体 = {BoldItem.IsChecked}，对齐 = {align}";
        }
    }
}
