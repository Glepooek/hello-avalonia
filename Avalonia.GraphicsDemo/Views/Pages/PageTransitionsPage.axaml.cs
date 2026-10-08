using System;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Avalonia.GraphicsDemo.Views.Pages
{
    public partial class PageTransitionsPage : UserControl
    {
        private static readonly string[] Colors = { "#4A7BE8", "#E8974A", "#4AE87B", "#E8564A" };
        private static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(400);

        private int _index;

        public PageTransitionsPage()
        {
            InitializeComponent();
            KindBox.SelectionChanged += (_, _) => Pager.PageTransition = CreateTransition();
            Pager.PageTransition = CreateTransition();
            Pager.Content = CreatePage();
        }

        private void OnNext(object? sender, RoutedEventArgs e)
        {
            _index++;

            // Replacing Content is what triggers the transition; a fresh control each time,
            // because one control instance cannot sit in two places at once.
            Pager.Content = CreatePage();
            Readout.Text = $"第 {_index % Colors.Length + 1} 页";
        }

        private Control CreatePage() => new Border
        {
            Background = new SolidColorBrush(Color.Parse(Colors[_index % Colors.Length])),
            Child = new TextBlock
            {
                Text = $"第 {_index % Colors.Length + 1} 页",
                FontSize = 24,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            },
        };

        private IPageTransition CreateTransition() => KindBox.SelectedIndex switch
        {
            0 => new PageSlide(Duration, PageSlide.SlideAxis.Horizontal),
            1 => new PageSlide(Duration, PageSlide.SlideAxis.Vertical),
            2 => new CrossFade(Duration),
            _ => new CompositePageTransition
            {
                PageTransitions =
                {
                    new PageSlide(Duration, PageSlide.SlideAxis.Horizontal),
                    new CrossFade(Duration),
                },
            },
        };
    }
}
