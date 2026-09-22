using Avalonia.Controls;
using Avalonia.FundamentalsDemo.ViewModels;
using Avalonia.Interactivity;

namespace Avalonia.FundamentalsDemo.Views.Pages
{
    public partial class TreesPage : UserControl
    {
        private readonly TreesViewModel _viewModel = new();

        public TreesPage()
        {
            InitializeComponent();
            DataContext = _viewModel;
        }

        private void OnSnapshotClick(object? sender, RoutedEventArgs e)
        {
            // Snapshot after the template has expanded, otherwise the visual
            // tree side would show nothing below the button itself.
            _viewModel.Refresh(SampleButton);
            HintText.Text = "快照已生成——展开左右两棵树对比层级深度。";
        }
    }
}
