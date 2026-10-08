using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Shared.Helpers;

namespace Avalonia.EventsDemo.Views.Pages
{
    public partial class LifecyclePage : UserControl
    {
        private readonly EventLog _log = new();
        private Button? _probed;

        public LifecyclePage()
        {
            InitializeComponent();
            LogList.ItemsSource = _log.Entries;
        }

        private void OnAdd(object? sender, RoutedEventArgs e)
        {
            if (_probed != null)
            {
                return;
            }

            _log.Clear();
            _probed = new Button { Content = "被观察的按钮", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };

            // Subscribe to all of them before attaching, otherwise the early ones are missed.
            _probed.AttachedToLogicalTree += (_, _) => _log.Write("AttachedToLogicalTree");
            _probed.AttachedToVisualTree += (_, _) => _log.Write("AttachedToVisualTree");
            _probed.Initialized += (_, _) => _log.Write("Initialized");
            _probed.SizeChanged += (_, _) => _log.Write("SizeChanged");
            _probed.Loaded += (_, _) => _log.Write("Loaded");
            _probed.Unloaded += (_, _) => _log.Write("Unloaded");
            _probed.DetachedFromVisualTree += (_, _) => _log.Write("DetachedFromVisualTree");
            _probed.DetachedFromLogicalTree += (_, _) => _log.Write("DetachedFromLogicalTree");

            Slot.Children.Add(_probed);
            AddButton.IsEnabled = false;
            RemoveButton.IsEnabled = true;
        }

        private void OnRemove(object? sender, RoutedEventArgs e)
        {
            if (_probed == null)
            {
                return;
            }

            Slot.Children.Remove(_probed);
            _probed = null;
            AddButton.IsEnabled = true;
            RemoveButton.IsEnabled = false;
        }

        private void OnClear(object? sender, RoutedEventArgs e) => _log.Clear();
    }
}
