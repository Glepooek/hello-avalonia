using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class NativeMenuPage : UserControl
    {
        private int _clicks;

        public NativeMenuPage()
        {
            InitializeComponent();

            var hello = new NativeMenuItem("演示：你好");
            hello.Click += (_, _) =>
            {
                _clicks++;
                ShowItems();
            };
            var root = new NativeMenuItem("演示")
            {
                Menu = new NativeMenu
                {
                    Items = { hello, new NativeMenuItemSeparator(), new NativeMenuItem("演示：关于") },
                },
            };
            Built = new NativeMenu { Items = { root } };

            AttachCheck.IsCheckedChanged += (_, _) =>
            {
                if (TopLevel.GetTopLevel(this) is { } top)
                {
                    NativeMenu.SetMenu(top, AttachCheck.IsChecked == true ? Built : null);
                    ShowExport();
                }
            };
            DetachedFromVisualTree += (_, e) =>
            {
                // Leave the window as it was, whatever the box says.
                if (AttachCheck.IsChecked == true && e.RootVisual is TopLevel top)
                {
                    NativeMenu.SetMenu(top, null);
                }
            };
            AttachedToVisualTree += (_, _) => ShowExport();
            ShowItems();
        }

        /// <summary>The menu the check box attaches; kept public so it can be inspected.</summary>
        public NativeMenu Built { get; }

        private void ShowExport()
        {
            var top = TopLevel.GetTopLevel(this);
            ExportText.Text = top is null
                ? "尚未加入窗口"
                : $"NativeMenu.GetIsNativeMenuExported = {NativeMenu.GetIsNativeMenuExported(top)}";
        }

        private void ShowItems() => ItemText.Text = $"菜单含 {Built.Items.Count} 个顶层项，点击「演示：你好」{_clicks} 次";
    }
}
