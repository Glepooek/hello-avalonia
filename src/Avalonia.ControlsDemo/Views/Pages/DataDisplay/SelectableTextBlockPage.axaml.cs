using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class SelectableTextBlockPage : UserControl
    {
        public SelectableTextBlockPage()
        {
            InitializeComponent();

            // The selection is not a plain property change notification, so the readout is refreshed after each button.
            SelectAllButton.Click += (_, _) => { Selectable.SelectAll(); Show(); };
            FirstWordButton.Click += (_, _) =>
            {
                Selectable.SelectionStart = 0;
                Selectable.SelectionEnd = 5;
                Show();
            };
            ClearButton.Click += (_, _) =>
            {
                Selectable.SelectionStart = 0;
                Selectable.SelectionEnd = 0;
                Show();
            };
            Selectable.PointerReleased += (_, _) => Show();
        }

        private void Show() =>
            SelectionText.Text = string.IsNullOrEmpty(Selectable.SelectedText) ? "SelectedText：（空）" : $"SelectedText：{Selectable.SelectedText}";
    }
}
