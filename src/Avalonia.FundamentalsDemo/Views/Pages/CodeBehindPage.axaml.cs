using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.FundamentalsDemo.Views.Pages
{
    public partial class CodeBehindPage : UserControl
    {
        // State living on the control is exactly what the MVVM page moves out.
        private int _count;

        public CodeBehindPage()
        {
            InitializeComponent();
        }

        private void OnGreetClick(object? sender, RoutedEventArgs e)
        {
            var name = NameBox.Text;
            ResultText.Text = string.IsNullOrWhiteSpace(name)
                ? "请先输入名字"
                : $"你好，{name.Trim()}！";
        }

        private void OnCountClick(object? sender, RoutedEventArgs e)
        {
            _count++;
            CountText.Text = $"{_count} 次";
        }
    }
}
