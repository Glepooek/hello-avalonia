using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.XamlDemo.Views.Pages
{
    public partial class DirectivesPage : UserControl
    {
        public DirectivesPage()
        {
            InitializeComponent();

            // The C# equivalent of x:Type. Shown here because a Binding cannot
            // take {x:Type} as its Source under compiled bindings.
            TypeText.Text = $"typeof(Button) 得到：{typeof(Button).FullName}";
        }

        private void OnReadClick(object? sender, RoutedEventArgs e)
        {
            // SourceBox is the field x:Name generated.
            ReadResult.Text = $"code-behind 读到：{SourceBox.Text}";
        }
    }
}
