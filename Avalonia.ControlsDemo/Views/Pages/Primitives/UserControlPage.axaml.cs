using Avalonia.Controls;
using Avalonia.Media;
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class UserControlPage : UserControl
    {
        private static readonly Dictionary<string, IBrush> Choices = new()
        {
            ["Gray"] = Brushes.Gray,
            ["Orange"] = Brushes.Orange,
            ["Purple"] = Brushes.MediumPurple,
            ["Gold"] = Brushes.Gold
        };

        public UserControlPage()
        {
            InitializeComponent();

            ColorBox.ItemsSource = Choices.Keys;
            ColorBox.SelectedItem = "Gray";
            ColorBox.SelectionChanged += (_, _) =>
            {
                if (ColorBox.SelectedItem is string key) Live.Fill = Choices[key];
                Refresh();
            };
            TitleBox.TextChanged += (_, _) =>
            {
                Live.Title = string.IsNullOrEmpty(TitleBox.Text) ? "可改的一块" : TitleBox.Text;
                Refresh();
            };

            Refresh();
        }

        private void Refresh()
        {
            StateText.Text = $"Live.Title = {Live.Title}，Live.Fill = {(Live.Fill as ISolidColorBrush)?.Color.ToString() ?? "（空）"}";
        }
    }
}
