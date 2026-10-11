using Avalonia.Animation;
using Avalonia.Controls;
using System;
using System.Linq;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class CarouselDemoPage : UserControl
    {
        public CarouselDemoPage()
        {
            InitializeComponent();

            Demo.Pages = new Page[]
            {
                DemoPages.Make("第 1 页", "轮播的第 1 页。"),
                DemoPages.Make("第 2 页", "轮播的第 2 页。"),
                DemoPages.Make("第 3 页", "轮播的第 3 页。")
            };

            PrevButton.Click += (_, _) => Demo.SelectedIndex = Math.Max(0, Demo.SelectedIndex - 1);
            NextButton.Click += (_, _) => Demo.SelectedIndex = Math.Min(2, Demo.SelectedIndex + 1);

            TransitionBox.ItemsSource = new[] { "无", "PageSlide", "CrossFade" };
            TransitionBox.SelectedIndex = 0;
            TransitionBox.SelectionChanged += (_, _) =>
            {
                Demo.PageTransition = TransitionBox.SelectedItem switch
                {
                    "PageSlide" => new PageSlide(TimeSpan.FromMilliseconds(300)),
                    "CrossFade" => new CrossFade(TimeSpan.FromMilliseconds(300)),
                    _ => null
                };
            };

            Demo.PropertyChanged += (_, e) =>
            {
                if (e.Property == SelectingMultiPage.SelectedIndexProperty) Refresh();
            };
            Refresh();
        }

        private void Refresh()
        {
            StateText.Text = $"SelectedIndex = {Demo.SelectedIndex}，共 {Demo.Pages?.Count() ?? 0} 页";
        }
    }
}
