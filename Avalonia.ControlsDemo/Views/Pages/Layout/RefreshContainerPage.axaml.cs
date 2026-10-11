using Avalonia.Controls;
using System;
using System.Threading.Tasks;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class RefreshContainerPage : UserControl
    {
        private int _count;

        public RefreshContainerPage()
        {
            InitializeComponent();

            Container.RefreshRequested += async (_, e) =>
            {
                // The visualizer keeps spinning until the deferral is completed.
                var deferral = e.GetDeferral();
                await Task.Delay(300);
                _count++;
                RefreshResult.Text = $"刷新 {_count} 次";
                Body.Text = $"内容：{DateTime.Now:HH:mm:ss} 刷新";
                deferral.Complete();
            };
            RefreshButton.Click += (_, _) => Container.RequestRefresh();
        }
    }
}
