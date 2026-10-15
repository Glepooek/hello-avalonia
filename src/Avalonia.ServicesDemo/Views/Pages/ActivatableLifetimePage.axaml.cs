using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using System;

namespace Avalonia.ServicesDemo.Views.Pages
{
    public partial class ActivatableLifetimePage : UserControl
    {
        private int _activated;
        private int _deactivated;
        private Window? _window;

        public ActivatableLifetimePage()
        {
            InitializeComponent();
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            // TryGetFeature returns null on platforms whose lifetime does not implement the service (Windows, Linux).
            var lifetime = Application.Current?.TryGetFeature<IActivatableLifetime>();
            AvailabilityLine.Text = lifetime is null
                ? "IActivatableLifetime：null（当前平台不支持，见下表）"
                : $"IActivatableLifetime：可用（{lifetime.GetType().Name}）";

            if (lifetime is not null)
            {
                lifetime.Activated += (_, args) => AvailabilityLine.Text = $"Activated：{args.Kind}";
                lifetime.Deactivated += (_, args) => AvailabilityLine.Text = $"Deactivated：{args.Kind}";
            }

            WindowLine.Text = "ActivationKind 取值：" + string.Join("、", Enum.GetNames<ActivationKind>());

            // Window-level activation exists everywhere a desktop window does.
            if (TopLevel.GetTopLevel(this) is Window window)
            {
                _window = window;
                window.Activated += OnWindowActivated;
                window.Deactivated += OnWindowDeactivated;
            }

            ShowCounts();
        }

        // Loaded runs again every time the tab is revisited, so drop the handlers here or each visit adds another pair.
        protected override void OnUnloaded(RoutedEventArgs e)
        {
            if (_window is not null)
            {
                _window.Activated -= OnWindowActivated;
                _window.Deactivated -= OnWindowDeactivated;
                _window = null;
            }

            base.OnUnloaded(e);
        }

        private void OnWindowActivated(object? sender, EventArgs e)
        {
            _activated++;
            ShowCounts();
        }

        private void OnWindowDeactivated(object? sender, EventArgs e)
        {
            _deactivated++;
            ShowCounts();
        }

        private void ShowCounts() => ActivationCounts.Text = $"Window.Activated 触发 {_activated} 次，Window.Deactivated 触发 {_deactivated} 次";
    }
}
