using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Linq;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class WindowDrawnDecorationsPage : UserControl
    {
        private Window? _sample;
        private bool _isCustom;

        public WindowDrawnDecorationsPage()
        {
            InitializeComponent();

            DecorationsBox.ItemsSource = Enum.GetValues<WindowDecorations>();
            DecorationsBox.SelectedItem = WindowDecorations.Full;
            ExtendCheck.IsChecked = false;

            DecorationsBox.SelectionChanged += (_, _) => ApplyToSystemSample();
            ExtendCheck.IsCheckedChanged += (_, _) => ApplyToSystemSample();
            OpenSystemButton.Click += (_, _) => OpenSystem();
            OpenCustomButton.Click += (_, _) => OpenCustom();
            CloseButton.Click += (_, _) => _sample?.Close();

            // Never leave an orphan window on the desktop when the user leaves this page.
            DetachedFromVisualTree += (_, _) => _sample?.Close();

            Refresh();
        }

        /// <summary>The sample window while it is open, otherwise null.</summary>
        public Window? Sample => _sample;

        private void OpenSystem()
        {
            var window = new Window
            {
                Title = "系统装饰示例",
                Width = 360,
                Height = 220,
                Content = new TextBlock
                {
                    Margin = new Thickness(16),
                    TextWrapping = TextWrapping.Wrap,
                    Text = "改页面上的 WindowDecorations 与 ExtendClientAreaToDecorationsHint，看这个窗口的标题栏与边框怎么变。"
                }
            };
            Attach(window, isCustom: false);
            ApplyToSystemSample();
            Show(window);
        }

        private void OpenCustom()
        {
            var title = new TextBlock
            {
                Text = "自绘标题栏",
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0)
            };
            var minimize = new Button { Content = "—", Width = 44 };
            var maximize = new Button { Content = "□", Width = 44 };
            var close = new Button { Content = "✕", Width = 44 };
            var buttons = new StackPanel { Orientation = Orientation.Horizontal, Children = { minimize, maximize, close } };
            DockPanel.SetDock(buttons, Dock.Right);

            var bar = new DockPanel { Height = 32, Background = Brushes.SteelBlue, Children = { buttons, title } };
            DockPanel.SetDock(bar, Dock.Top);

            WindowDecorationProperties.SetElementRole(bar, WindowDecorationsElementRole.TitleBar);
            WindowDecorationProperties.SetElementRole(minimize, WindowDecorationsElementRole.MinimizeButton);
            WindowDecorationProperties.SetElementRole(maximize, WindowDecorationsElementRole.MaximizeButton);
            WindowDecorationProperties.SetElementRole(close, WindowDecorationsElementRole.CloseButton);

            var window = new Window
            {
                Title = "自绘标题栏示例",
                Width = 360,
                Height = 220,
                WindowDecorations = WindowDecorations.None,
                Content = new DockPanel
                {
                    Children =
                    {
                        bar,
                        new TextBlock { Margin = new Thickness(16), TextWrapping = TextWrapping.Wrap, Text = "拖蓝色的标题栏移动窗口，三个按钮分别最小化、最大化与关闭。" }
                    }
                }
            };
            Attach(window, isCustom: true);

            RolesText.Text = "已标记的角色（读回）：" + string.Join("、",
                new Control[] { bar, minimize, maximize, close }.Select(c => WindowDecorationProperties.GetElementRole(c).ToString()));
            Show(window);
        }

        private void Attach(Window window, bool isCustom)
        {
            _sample?.Close();
            _sample = window;
            _isCustom = isCustom;
            window.Closed += (_, _) =>
            {
                if (ReferenceEquals(_sample, window)) _sample = null;
                Refresh();
            };
            window.PropertyChanged += (_, _) => Refresh();
        }

        private void Show(Window window)
        {
            if (TopLevel.GetTopLevel(this) is Window owner)
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.Show(owner);
            }
            else
            {
                window.Show();
            }
            Refresh();
        }

        private void ApplyToSystemSample()
        {
            if (_sample is null || _isCustom) return;

            if (DecorationsBox.SelectedItem is WindowDecorations decorations) _sample.WindowDecorations = decorations;
            _sample.ExtendClientAreaToDecorationsHint = ExtendCheck.IsChecked == true;
            Refresh();
        }

        private void Refresh()
        {
            StateText.Text = _sample is null
                ? "示例窗口：未打开"
                : $"WindowDecorations = {_sample.WindowDecorations}，ExtendClientAreaToDecorationsHint = {_sample.ExtendClientAreaToDecorationsHint}，" +
                  $"IsExtendedIntoWindowDecorations = {_sample.IsExtendedIntoWindowDecorations}，WindowDecorationMargin = {_sample.WindowDecorationMargin}";
        }
    }
}
