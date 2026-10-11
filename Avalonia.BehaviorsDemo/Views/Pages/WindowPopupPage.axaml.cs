using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.BehaviorsDemo.Views.Pages
{
    public partial class WindowPopupPage : UserControl
    {
        public WindowPopupPage()
        {
            InitializeComponent();
        }

        // The most recently opened child window; exposed so a probe can reach it.
        public ChildWindow? Child { get; private set; }

        private void OnOpenChild(object? sender, RoutedEventArgs e)
        {
            Child = new ChildWindow();
            if (TopLevel.GetTopLevel(this) is Window owner)
            {
                Child.Show(owner);
            }
            else
            {
                Child.Show();
            }
        }
    }
}
