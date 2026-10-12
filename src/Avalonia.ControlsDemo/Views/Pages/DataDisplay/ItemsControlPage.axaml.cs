using Avalonia.Controls;
using Avalonia.Controls.Templates;
using System.Collections.ObjectModel;
using System.Linq;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ItemsControlPage : UserControl
    {
        private readonly ObservableCollection<string> _source = ["Apple", "Bread", "Cherry"];
        private int _next = 1;

        public ItemsControlPage()
        {
            InitializeComponent();

            Items.ItemsSource = _source;
            Show();

            AddButton.Click += (_, _) => { _source.Add($"新项 {_next++}"); Show(); };
            RemoveButton.Click += (_, _) =>
            {
                if (_source.Count > 0)
                {
                    _source.RemoveAt(_source.Count - 1);
                    Show();
                }
            };

            PanelBox.ItemsSource = new[] { "StackPanel", "WrapPanel" };
            PanelBox.SelectedIndex = 0;
            PanelBox.SelectionChanged += (_, _) =>
                Items.ItemsPanel = PanelBox.SelectedIndex == 1
                    ? new FuncTemplate<Panel?>(() => new WrapPanel())
                    : new FuncTemplate<Panel?>(() => new StackPanel());
        }

        private void Show() => CountText.Text = $"ItemCount = {Items.ItemCount}";
    }
}
