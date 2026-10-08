using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.VisualTree;
using System;
using System.Linq;

namespace Avalonia.ServicesDemo.Views.Pages
{
    public partial class PlatformSettingsPage : UserControl
    {
        private IPlatformSettings? _settings;

        public PlatformSettingsPage()
        {
            InitializeComponent();
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            // GetPlatformSettings is an extension on Visual; there is no TopLevel.PlatformSettings property.
            _settings = this.GetPlatformSettings();
            if (_settings is null)
            {
                ThemeLine.Text = "GetPlatformSettings() 返回 null";
                return;
            }

            ShowColors(_settings.GetColorValues());
            _settings.ColorValuesChanged += OnColorValuesChanged;

            TapLine.Text = $"GetTapSize(Mouse) = {_settings.GetTapSize(PointerType.Mouse)}";
            DoubleTapLine.Text = $"GetDoubleTapSize(Mouse) = {_settings.GetDoubleTapSize(PointerType.Mouse)}，GetDoubleTapTime(Mouse) = {_settings.GetDoubleTapTime(PointerType.Mouse).TotalMilliseconds} ms";
            HoldLine.Text = $"HoldWaitDuration = {_settings.HoldWaitDuration.TotalMilliseconds} ms（按下到 Holding 事件的延迟）";

            var keys = _settings.HotkeyConfiguration;
            CopyLine.Text = "Copy：" + string.Join("、", keys.Copy);
            PasteLine.Text = "Paste：" + string.Join("、", keys.Paste);
            UndoLine.Text = "Undo：" + string.Join("、", keys.Undo);
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            // The service outlives the page, so unsubscribe or the handler keeps this control alive.
            if (_settings is not null)
            {
                _settings.ColorValuesChanged -= OnColorValuesChanged;
            }

            base.OnUnloaded(e);
        }

        private void OnColorValuesChanged(object? sender, PlatformColorValues values)
        {
            // The event may arrive on a platform thread.
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                ShowColors(values);
                ChangeLine.Text = $"{DateTime.Now:HH:mm:ss} 系统颜色已变化 → {values.ThemeVariant}";
            });
        }

        private void ShowColors(PlatformColorValues values)
        {
            ThemeLine.Text = $"ThemeVariant = {values.ThemeVariant}";
            ContrastLine.Text = $"ContrastPreference = {values.ContrastPreference}";
            AccentSwatch.Background = new SolidColorBrush(values.AccentColor1);
            AccentLine.Text = values.AccentColor1.ToString();
        }
    }
}
