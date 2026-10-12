using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class GridHowToPage : UserControl
    {
        public GridHowToPage()
        {
            InitializeComponent();
            LinesCheck.IsCheckedChanged += (_, _) => SpanGrid.ShowGridLines = LinesCheck.IsChecked == true;
        }
    }
}
