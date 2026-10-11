using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class TabStripPage : UserControl
    {
        public TabStripPage()
        {
            InitializeComponent();

            Strip.SelectionChanged += (_, _) => Refresh();
            LastButton.Click += (_, _) => Strip.SelectedIndex = Strip.ItemCount - 1;
            Refresh();
        }

        private void Refresh()
        {
            string label = (Strip.SelectedItem as TabStripItem)?.Content?.ToString() ?? "（无）";
            ContentText.Text = $"SelectedIndex = {Strip.SelectedIndex}，当前显示「{label}」的内容";
        }
    }
}
