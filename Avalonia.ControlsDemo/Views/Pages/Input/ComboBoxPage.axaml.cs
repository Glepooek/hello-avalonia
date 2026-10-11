using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ComboBoxPage : UserControl
    {
        private sealed record Country(string Code, string Name);

        public ComboBoxPage()
        {
            InitializeComponent();

            Basic.SelectionChanged += (_, _) =>
                BasicResult.Text = Basic.SelectedItem is ComboBoxItem item ? $"已选：{item.Content}" : "尚未选择";

            Editable.PropertyChanged += (_, e) =>
            {
                if (e.Property == ComboBox.TextProperty)
                {
                    EditableResult.Text = $"Text = {(string.IsNullOrEmpty(Editable.Text) ? "(空)" : Editable.Text)}";
                }
            };

            ByValue.ItemsSource = new[]
            {
                new Country("CN", "中国"),
                new Country("JP", "日本"),
                new Country("DE", "德国"),
            };
            ByValue.SelectionChanged += (_, _) =>
                ValueResult.Text = $"SelectedValue = {ByValue.SelectedValue ?? "(空)"}";
        }
    }
}
