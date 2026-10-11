using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class LabelPage : UserControl
    {
        public LabelPage()
        {
            InitializeComponent();

            NameBox.GotFocus += (_, _) => FocusText.Text = "输入框：已聚焦";
            NameBox.LostFocus += (_, _) => FocusText.Text = "输入框：未聚焦";
        }
    }
}
