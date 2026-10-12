using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using System;

namespace Avalonia.FundamentalsDemo.Views.Pages
{
    public partial class LifetimesPage : UserControl
    {
        public LifetimesPage()
        {
            InitializeComponent();
            DescribeLifetime();
            DescribeAsset();
        }

        private void DescribeLifetime()
        {
            LifetimeText.Text = Application.Current?.ApplicationLifetime switch
            {
                IClassicDesktopStyleApplicationLifetime => "当前：桌面生命周期（IClassicDesktopStyleApplicationLifetime）",
                ISingleViewApplicationLifetime => "当前：单视图生命周期（ISingleViewApplicationLifetime）",
                null => "当前：没有生命周期（设计器或测试宿主）",
                var other => $"当前：{other.GetType().Name}",
            };

            UpdateWindowCount();
        }

        private void UpdateWindowCount()
        {
            WindowCountText.Text =
                Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                    ? $"已打开窗口数：{desktop.Windows.Count}"
                    : "非桌面生命周期，没有窗口列表";
        }

        private void OnOpenWindowClick(object? sender, RoutedEventArgs e)
        {
            var extra = new Window
            {
                Title = "多开的窗口",
                Width = 320,
                Height = 200,
                Content = new TextBlock
                {
                    Margin = new Thickness(16),
                    TextWrapping = TextWrapping.Wrap,
                    Text = "桌面生命周期允许任意多个窗口。关掉它再点『刷新』，窗口数会变回去。",
                },
            };

            extra.Closed += (_, _) => UpdateWindowCount();
            extra.Show();
            UpdateWindowCount();
        }

        private void DescribeAsset()
        {
            // Assets are embedded in the assembly, addressed by the avares scheme.
            var uri = new Uri("avares://Avalonia.FundamentalsDemo/Assets/avalonia-logo.ico");
            using var stream = AssetLoader.Open(uri);
            AssetText.Text = $"已通过 AssetLoader 打开图标资源，共 {stream.Length} 字节。";
        }
    }
}
