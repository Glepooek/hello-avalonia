using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;

namespace Avalonia.AppDevelopmentDemo.Views.Pages
{
    public partial class NativeInteropPage : UserControl
    {
        public NativeInteropPage()
        {
            InitializeComponent();
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            RefreshPlatformHandle();
        }

        internal void RefreshPlatformHandle()
        {
            var handle = TopLevel.GetTopLevel(this)?.TryGetPlatformHandle();
            PlatformHandleText.Text = handle is null
                ? "原生句柄：当前平台或测试宿主未提供"
                : $"原生句柄：{handle.HandleDescriptor} = 0x{handle.Handle.ToInt64():X}";
        }
    }
}
