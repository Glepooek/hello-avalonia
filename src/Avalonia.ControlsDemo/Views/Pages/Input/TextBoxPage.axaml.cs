using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class TextBoxPage : UserControl
    {
        public TextBoxPage()
        {
            InitializeComponent();

            Basic.TextChanged += (_, _) => BasicResult.Text = $"长度 {Basic.Text?.Length ?? 0}";
            RevealCheck.IsCheckedChanged += (_, _) => Password.RevealPassword = RevealCheck.IsChecked == true;

            SelectButton.Click += (_, _) =>
            {
                Selectable.Focus();
                Selectable.SelectionStart = 0;
                Selectable.SelectionEnd = 7;
                SelectionResult.Text = $"选区：「{Selectable.SelectedText}」";
            };
        }
    }
}
