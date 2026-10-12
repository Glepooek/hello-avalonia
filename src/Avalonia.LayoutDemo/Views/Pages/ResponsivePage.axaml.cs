using Avalonia.Controls;
using Avalonia.LayoutDemo.ViewModels;

namespace Avalonia.LayoutDemo.Views.Pages
{
    public partial class ResponsivePage : UserControl
    {
        private readonly ResponsiveViewModel _viewModel = new();

        public ResponsivePage()
        {
            InitializeComponent();
            DataContext = _viewModel;

            // SizeChanged is the hook for the breakpoint-view-model technique:
            // container queries cannot express non-size conditions, this can.
            SizeChanged += (_, e) => _viewModel.UpdateLayout(e.NewSize.Width);
        }
    }
}
