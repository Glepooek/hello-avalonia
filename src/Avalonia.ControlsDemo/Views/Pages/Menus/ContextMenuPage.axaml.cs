using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ContextMenuPage : UserControl
    {
        private int _opening;
        private int _opened;
        private int _closing;

        public ContextMenuPage()
        {
            InitializeComponent();

            Menu.Opening += (_, e) =>
            {
                _opening++;
                e.Cancel = CancelCheck.IsChecked == true;
                Show();
            };
            Menu.Opened += (_, _) =>
            {
                _opened++;
                Show();
            };
            Menu.Closing += (_, _) =>
            {
                _closing++;
                Show();
            };

            OpenButton.Click += (_, _) =>
            {
                if (Menu.IsOpen)
                {
                    Menu.Close();
                }
                else
                {
                    Menu.Open(Area);
                }
            };
        }

        private void Show() => LogText.Text = $"Opening {_opening} 次，Opened {_opened} 次，Closing {_closing} 次";
    }
}
