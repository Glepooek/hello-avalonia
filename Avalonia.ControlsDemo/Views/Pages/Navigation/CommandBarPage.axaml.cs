using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Linq;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class CommandBarPage : UserControl
    {
        public CommandBarPage()
        {
            InitializeComponent();

            LabelBox.ItemsSource = Enum.GetValues<CommandBarDefaultLabelPosition>();
            LabelBox.SelectedItem = Bar.DefaultLabelPosition;
            LabelBox.SelectionChanged += (_, _) =>
            {
                if (LabelBox.SelectedItem is CommandBarDefaultLabelPosition position)
                {
                    Bar.DefaultLabelPosition = position;
                }
            };

            Bar.AddHandler(Button.ClickEvent, OnBarClick);

            OverflowCheck.IsChecked = OverflowBar.IsDynamicOverflowEnabled;
            OverflowCheck.IsCheckedChanged += (_, _) =>
            {
                OverflowBar.IsDynamicOverflowEnabled = OverflowCheck.IsChecked == true;
                RefreshOverflow();
            };
            WidthSlider.ValueChanged += (_, e) =>
            {
                OverflowHost.Width = e.NewValue;
                RefreshOverflow();
            };
            RefreshOverflow();
        }

        private void OnBarClick(object? sender, RoutedEventArgs e)
        {
            string? label = (e.Source as CommandBarButton)?.Label ?? (e.Source as CommandBarToggleButton)?.Label;
            if (label is not null)
            {
                ClickText.Text = $"最近点击：{label}";
            }
        }

        // Overflow is decided during layout, so read it after the layout pass.
        private void RefreshOverflow()
        {
            Dispatcher.UIThread.Post(() =>
            {
                int overflowed = OverflowBar.PrimaryCommands.OfType<ICommandBarElement>().Count(c => c.IsInOverflow);
                OverflowText.Text = $"IsDynamicOverflowEnabled = {OverflowBar.IsDynamicOverflowEnabled}，进入溢出菜单的主命令：{overflowed} 个";
            }, DispatcherPriority.Loaded);
        }
    }
}
