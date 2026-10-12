using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using System.Linq;

namespace Avalonia.AppDevelopmentDemo.Views.Pages
{
    public partial class WindowManagementPage : UserControl
    {
        public WindowManagementPage()
        {
            InitializeComponent();
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            Describe();
        }

        private Window? Host => TopLevel.GetTopLevel(this) as Window;

        private void Describe()
        {
            if (Host is not { } w)
                return;
            StateLine.Text = $"WindowState = {w.WindowState}，CanResize = {w.CanResize}，SizeToContent = {w.SizeToContent}，Topmost = {w.Topmost}";
            DecorationsLine.Text = $"WindowDecorations = {w.WindowDecorations}，ShowInTaskbar = {w.ShowInTaskbar}";
            var screens = w.Screens;
            ScreenLine.Text = screens is null
                ? "Screens = null"
                : $"屏幕 {screens.All.Count} 个，主屏工作区 = {screens.Primary?.WorkingArea}";
        }

        private static Window MakeChild(string title, Control? extra = null)
        {
            var panel = new StackPanel { Margin = new Thickness(16), Spacing = 8 };
            panel.Children.Add(new TextBlock { Text = title });
            if (extra is not null)
                panel.Children.Add(extra);
            return new Window
            {
                Title = title,
                Width = 320,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = panel,
            };
        }

        private void OnShow(object? sender, RoutedEventArgs e)
        {
            if (Host is not { } owner)
                return;
            var child = MakeChild("非模态子窗口");
            child.Show(owner);
            Result.Text = $"Show(owner) 之后 child.Owner 是主窗口：{ReferenceEquals(child.Owner, owner)}";
        }

        private async void OnDialog(object? sender, RoutedEventArgs e)
        {
            if (Host is not { } owner)
                return;
            var ok = new Button { Content = "确定，返回 \"accepted\"" };
            var dialog = MakeChild("模态对话框", ok);
            ok.Click += (_, _) => dialog.Close("accepted");

            var answer = await dialog.ShowDialog<string?>(owner);
            // Closing with the title-bar button returns the default, null.
            Result.Text = $"ShowDialog 返回：{answer ?? "null"}";
        }

        private void OnGuard(object? sender, RoutedEventArgs e)
        {
            if (Host is not { } owner)
                return;
            var allow = false;
            var allowBox = new CheckBox { Content = "允许关闭" };
            allowBox.IsCheckedChanged += (_, _) => allow = allowBox.IsChecked == true;
            var child = MakeChild("拦截关闭", allowBox);
            child.Closing += (_, args) =>
            {
                if (!allow)
                {
                    args.Cancel = true;
                    Result.Text = "Closing 里 e.Cancel = true，窗口还在；勾上「允许关闭」再关";
                }
            };
            child.Show(owner);
        }
    }
}
