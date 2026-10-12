using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class NotificationPage : UserControl
    {
        private int _sent;
        private int _clicked;

        public NotificationPage()
        {
            InitializeComponent();

            TypeBox.ItemsSource = Enum.GetValues<NotificationType>();
            TypeBox.SelectedItem = NotificationType.Information;
            PositionBox.ItemsSource = Enum.GetValues<NotificationPosition>();
            PositionBox.SelectedItem = NotificationPosition.TopRight;
            PositionBox.SelectionChanged += (_, _) =>
            {
                if (Manager is not null && PositionBox.SelectedItem is NotificationPosition position)
                {
                    Manager.Position = position;
                }
            };

            MaxItemsSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    MaxItemsText.Text = ((int)MaxItemsSlider.Value).ToString();
                    if (Manager is not null)
                    {
                        Manager.MaxItems = (int)MaxItemsSlider.Value;
                    }
                }
            };
            SecondsSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    SecondsText.Text = ((int)SecondsSlider.Value).ToString();
                }
            };

            ShowButton.Click += (_, _) => Send(null);
            ClickableButton.Click += (_, _) => Send(() =>
            {
                _clicked++;
                ShowCounts();
            });
            CloseAllButton.Click += (_, _) => Manager?.CloseAll();

            AttachedToVisualTree += (_, _) =>
            {
                // The manager needs the window, so it can only be made once the page is on screen.
                if (Manager is null && TopLevel.GetTopLevel(this) is { } top)
                {
                    Manager = new WindowNotificationManager(top)
                    {
                        Position = (NotificationPosition)PositionBox.SelectedItem!,
                        MaxItems = (int)MaxItemsSlider.Value,
                    };
                }
            };
            DetachedFromVisualTree += (_, _) =>
            {
                Manager?.CloseAll();
            };
        }

        /// <summary>The window-level manager; null until the page is attached.</summary>
        public WindowNotificationManager? Manager { get; private set; }

        private void Send(Action? onClick)
        {
            if (Manager is null)
            {
                return;
            }

            var type = (NotificationType)TypeBox.SelectedItem!;
            _sent++;
            Manager.Show(new Notification($"第 {_sent} 条", $"这是一条 {type} 通知", type,
                TimeSpan.FromSeconds((int)SecondsSlider.Value), onClick));
            ShowCounts();
        }

        private void ShowCounts() => SentText.Text = $"已发送 {_sent} 条，点击 {_clicked} 次";
    }
}
