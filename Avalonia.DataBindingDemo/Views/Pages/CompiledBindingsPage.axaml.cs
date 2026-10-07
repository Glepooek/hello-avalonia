using Avalonia.Controls;
using Avalonia.Data;

namespace Avalonia.DataBindingDemo.Views.Pages
{
    public partial class CompiledBindingsPage : UserControl
    {
        public CompiledBindingsPage()
        {
            InitializeComponent();

            // The code-side equivalent of Text="{Binding #Source.Value, StringFormat=...}".
            CodeBoundText.Bind(TextBlock.TextProperty, new Binding("Value")
            {
                Source = Source,
                StringFormat = "代码里 Bind() 读到 {0:F1}",
            });
        }
    }
}
