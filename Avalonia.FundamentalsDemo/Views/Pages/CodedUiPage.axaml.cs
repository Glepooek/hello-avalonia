using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Avalonia.FundamentalsDemo.Views.Pages
{
    public partial class CodedUiPage : UserControl
    {
        public CodedUiPage()
        {
            InitializeComponent();

            // The same three controls the XAML side declares, built by hand.
            // Property setters map one-to-one onto XAML attributes.
            CodeBuiltHost.Child = new StackPanel
            {
                Spacing = 6,
                Children =
                {
                    new TextBlock { FontWeight = FontWeight.SemiBold, Text = "C# 构建" },
                    new TextBox { PlaceholderText = "输入点什么" },
                    new Button
                    {
                        Content = "提交",
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                    },
                },
            };
        }
    }
}
