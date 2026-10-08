using Avalonia.Controls;
using Avalonia.Input;

namespace Avalonia.GraphicsDemo.Views.Pages
{
    public partial class ClipAndHitPage : UserControl
    {
        private int _noBrush, _clear, _invisible;

        public ClipAndHitPage()
        {
            InitializeComponent();
            Show();
        }

        private void OnTapped(object? sender, TappedEventArgs e)
        {
            switch ((sender as Control)?.Name)
            {
                case "NoBrush": _noBrush++; break;
                case "Clear": _clear++; break;
                case "Invisible": _invisible++; break;
            }

            Show();
        }

        private void Show()
            => HitReadout.Text = $"没有 Background={_noBrush}   Transparent={_clear}   IsHitTestVisible=False={_invisible}";
    }
}
