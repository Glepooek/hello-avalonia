using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class GridSplitterPage : UserControl
    {
        public GridSplitterPage()
        {
            InitializeComponent();

            // Dragging rewrites ColumnDefinition.Width, so the readout reads it back after every layout pass.
            Split.LayoutUpdated += (_, _) => WidthText.Text = $"左栏宽度 {Split.ColumnDefinitions[0].Width.Value:F0}";
            PreviewCheck.IsCheckedChanged += (_, _) => Splitter.ShowsPreview = PreviewCheck.IsChecked == true;
        }
    }
}
