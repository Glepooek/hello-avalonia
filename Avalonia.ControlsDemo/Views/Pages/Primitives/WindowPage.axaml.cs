using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Linq;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class WindowPage : UserControl
    {
        private Window? _sample;
        private bool _forceClose;
        private bool _syncing;

        public WindowPage()
        {
            InitializeComponent();

            ResizeCheck.IsChecked = true;
            MinimizeCheck.IsChecked = true;
            MaximizeCheck.IsChecked = true;
            TaskbarCheck.IsChecked = true;

            StateBox.ItemsSource = Enum.GetValues<WindowState>();
            StateBox.SelectedItem = WindowState.Normal;
            SizeBox.ItemsSource = Enum.GetValues<SizeToContent>();
            SizeBox.SelectedItem = SizeToContent.Manual;
            LocationBox.ItemsSource = Enum.GetValues<WindowStartupLocation>();
            LocationBox.SelectedItem = WindowStartupLocation.CenterOwner;

            OpenButton.Click += (_, _) => OpenSample();
            CloseButton.Click += (_, _) => _sample?.Close();

            ResizeCheck.IsCheckedChanged += (_, _) => ApplyToSample();
            MinimizeCheck.IsCheckedChanged += (_, _) => ApplyToSample();
            MaximizeCheck.IsCheckedChanged += (_, _) => ApplyToSample();
            TopmostCheck.IsCheckedChanged += (_, _) => ApplyToSample();
            TaskbarCheck.IsCheckedChanged += (_, _) => ApplyToSample();
            StateBox.SelectionChanged += (_, _) => ApplyToSample();
            SizeBox.SelectionChanged += (_, _) => ApplyToSample();

            // Never leave an orphan window on the desktop when the user leaves this page.
            DetachedFromVisualTree += (_, _) => CloseSample();

            Refresh();
        }

        /// <summary>The sample window while it is open, otherwise null.</summary>
        public Window? Sample => _sample;

        /// <summary>How many times the sample window raised Closing since it was last opened.</summary>
        public int ClosingCount { get; private set; }

        private void OpenSample()
        {
            if (_sample is not null)
            {
                _sample.Activate();
                return;
            }

            var window = new Window
            {
                Title = "示例窗口",
                Width = 320,
                Height = 200,
                WindowStartupLocation = LocationBox.SelectedItem is WindowStartupLocation location ? location : WindowStartupLocation.CenterOwner,
                Content = new TextBlock
                {
                    Margin = new Thickness(16),
                    TextWrapping = TextWrapping.Wrap,
                    Text = "这是用 Show(owner) 打开的示例窗口。回到页面改它的属性。"
                }
            };
            ClosingCount = 0;
            _sample = window;

            window.Closing += OnSampleClosing;
            window.Closed += (_, _) =>
            {
                if (ReferenceEquals(_sample, window)) _sample = null;
                Refresh();
            };
            window.Opened += (_, _) => Refresh();
            window.PositionChanged += (_, _) => Refresh();
            window.Resized += (_, _) => Refresh();
            window.PropertyChanged += (_, e) =>
            {
                // The user can minimise or maximise from the title bar, so mirror it back into the box.
                if (e.Property == Window.WindowStateProperty)
                {
                    _syncing = true;
                    StateBox.SelectedItem = window.WindowState;
                    _syncing = false;
                    Refresh();
                }
            };

            ApplyToSample();
            if (TopLevel.GetTopLevel(this) is Window owner)
            {
                window.Show(owner);
            }
            else
            {
                window.Show();
            }
            Refresh();
        }

        private void OnSampleClosing(object? sender, WindowClosingEventArgs e)
        {
            ClosingCount++;
            // Only the user's own close is held back; closing the owner or shutting down must go through.
            if (BlockCheck.IsChecked == true && !_forceClose && e.CloseReason == WindowCloseReason.WindowClosing)
            {
                e.Cancel = true;
            }
            Refresh();
        }

        private void CloseSample()
        {
            _forceClose = true;
            _sample?.Close();
            _forceClose = false;
        }

        private void ApplyToSample()
        {
            if (_sample is null || _syncing) return;

            _sample.CanResize = ResizeCheck.IsChecked == true;
            _sample.CanMinimize = MinimizeCheck.IsChecked == true;
            _sample.CanMaximize = MaximizeCheck.IsChecked == true;
            _sample.Topmost = TopmostCheck.IsChecked == true;
            _sample.ShowInTaskbar = TaskbarCheck.IsChecked == true;
            if (StateBox.SelectedItem is WindowState state) _sample.WindowState = state;
            if (SizeBox.SelectedItem is SizeToContent size) _sample.SizeToContent = size;
            Refresh();
        }

        private void Refresh()
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            // Closed fires while the window is still in OwnedWindows, so count only the ones still showing.
            OwnerText.Text = $"所有者的 OwnedWindows 数：{owner?.OwnedWindows.Count(w => w.IsVisible) ?? 0}";

            if (_sample is null)
            {
                StateText.Text = ClosingCount == 0 ? "示例窗口：未打开" : $"示例窗口：已关闭（上次共触发 Closing {ClosingCount} 次）";
                return;
            }

            StateText.Text = $"示例窗口：IsVisible = {_sample.IsVisible}，WindowState = {_sample.WindowState}，Position = {_sample.Position}，" +
                             $"大小 = {_sample.Bounds.Width:0}×{_sample.Bounds.Height:0}，Closing 触发 {ClosingCount} 次";
        }
    }
}
