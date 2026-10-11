using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ScrollViewerPage : UserControl
    {
        public ScrollViewerPage()
        {
            InitializeComponent();

            HBox.ItemsSource = Enum.GetValues<ScrollBarVisibility>();
            HBox.SelectedItem = Scroller.HorizontalScrollBarVisibility;
            HBox.SelectionChanged += (_, _) =>
            {
                if (HBox.SelectedItem is ScrollBarVisibility v)
                {
                    Scroller.HorizontalScrollBarVisibility = v;
                }
            };
            VBox.ItemsSource = Enum.GetValues<ScrollBarVisibility>();
            VBox.SelectedItem = Scroller.VerticalScrollBarVisibility;
            VBox.SelectionChanged += (_, _) =>
            {
                if (VBox.SelectedItem is ScrollBarVisibility v)
                {
                    Scroller.VerticalScrollBarVisibility = v;
                }
            };

            HomeButton.Click += (_, _) => Scroller.ScrollToHome();
            EndButton.Click += (_, _) => Scroller.ScrollToEnd();
            LineButton.Click += (_, _) => Scroller.LineDown();
            Scroller.ScrollChanged += (_, _) => OffsetText.Text = $"Offset = {Scroller.Offset.X:F0}, {Scroller.Offset.Y:F0}";
        }
    }
}
