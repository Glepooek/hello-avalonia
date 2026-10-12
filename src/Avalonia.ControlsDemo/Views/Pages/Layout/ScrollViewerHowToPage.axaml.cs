using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ScrollViewerHowToPage : UserControl
    {
        private const int MaxItems = 60;
        private int _loaded;

        public ScrollViewerHowToPage()
        {
            InitializeComponent();

            AddItems(20);
            FeedScroller.ScrollChanged += (_, _) =>
            {
                // Extent is 0 before the first layout, which would also satisfy the "at bottom" test, so require real overflow.
                var overflows = FeedScroller.Extent.Height > FeedScroller.Viewport.Height;
                var atBottom = FeedScroller.Offset.Y + FeedScroller.Viewport.Height >= FeedScroller.Extent.Height - 1;
                if (overflows && atBottom && _loaded < MaxItems)
                {
                    AddItems(10);
                }
            };
            LoadButton.Click += (_, _) => FeedScroller.ScrollToEnd();

            JumpButton.Click += (_, _) => JumpTarget.BringIntoView();
            ChainCheck.IsCheckedChanged += (_, _) => ChainInner.IsScrollChainingEnabled = ChainCheck.IsChecked == true;
        }

        private void AddItems(int count)
        {
            for (var i = 0; i < count; i++)
            {
                FeedList.Children.Add(new TextBlock { Text = $"第 {++_loaded} 项" });
            }

            FeedCount.Text = $"已加载 {_loaded} 项";
        }
    }
}
