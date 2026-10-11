using Avalonia.Controls;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.Input;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class TrayIconPage : UserControl
    {
        private TrayIcon? _tray;
        private TrayIcons? _previous;
        private NativeMenuItem? _toggleItem;
        private int _showClicks, _toggleClicks, _quitClicks, _iconClicks;

        public TrayIconPage()
        {
            InitializeComponent();

            VisibleCheck.IsChecked = true;
            TipBox.Text = "ControlsDemo 托盘图标";
            VisibleCheck.IsCheckedChanged += (_, _) =>
            {
                if (_tray is not null) _tray.IsVisible = VisibleCheck.IsChecked == true;
                Refresh();
            };
            TipBox.TextChanged += (_, _) =>
            {
                if (_tray is not null) _tray.ToolTipText = TipBox.Text;
                Refresh();
            };

            // Icons are attached to the Application, so tie their lifetime to this page being on screen.
            AttachedToVisualTree += (_, _) => Install();
            DetachedFromVisualTree += (_, _) => Uninstall();

            Refresh();
        }

        /// <summary>The tray icon while this page is on screen, otherwise null.</summary>
        public TrayIcon? Tray => _tray;

        private void Install()
        {
            if (_tray is not null || Application.Current is not { } app) return;

            var show = new NativeMenuItem { Header = "显示", Command = new RelayCommand(() => { _showClicks++; Refresh(); }) };
            _toggleItem = new NativeMenuItem
            {
                Header = "开机提示",
                ToggleType = MenuItemToggleType.CheckBox,
                IsChecked = true,
                Command = new RelayCommand(() => { _toggleClicks++; Refresh(); })
            };
            var quit = new NativeMenuItem { Header = "退出（演示用，不会真的退出）", Command = new RelayCommand(() => { _quitClicks++; Refresh(); }) };

            using var stream = AssetLoader.Open(new Uri("avares://Avalonia.ControlsDemo/Assets/avalonia-logo.ico"));
            _tray = new TrayIcon
            {
                Icon = new WindowIcon(stream),
                ToolTipText = TipBox.Text,
                IsVisible = VisibleCheck.IsChecked == true,
                Menu = new NativeMenu { Items = { show, new NativeMenuItemSeparator(), _toggleItem, quit } }
            };
            _tray.Clicked += (_, _) =>
            {
                _iconClicks++;
                Refresh();
            };

            _previous = TrayIcon.GetIcons(app);
            TrayIcon.SetIcons(app, new TrayIcons { _tray });
            Refresh();
        }

        private void Uninstall()
        {
            if (_tray is null) return;

            _tray.Dispose();
            if (Application.Current is { } app) TrayIcon.SetIcons(app, _previous);
            _tray = null;
            _toggleItem = null;
            Refresh();
        }

        private void Refresh()
        {
            StateText.Text = _tray is null
                ? "托盘图标：未挂上（页面不在屏幕上）"
                : $"托盘图标：已挂上，IsVisible = {_tray.IsVisible}，ToolTipText = {_tray.ToolTipText}，菜单项 {_tray.Menu?.Items.Count} 个，" +
                  $"NativeMenuExporter = {(_tray.NativeMenuExporter is null ? "无（当前平台没有原生托盘实现）" : "有")}";
            MenuText.Text = $"菜单点击：显示 {_showClicks} 次，开机提示 {_toggleClicks} 次（IsChecked = {_toggleItem?.IsChecked.ToString() ?? "-"}），退出 {_quitClicks} 次；" +
                            $"图标本身被点击 {_iconClicks} 次（Clicked）";
        }
    }
}
