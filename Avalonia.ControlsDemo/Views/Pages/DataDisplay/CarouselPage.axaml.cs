using Avalonia.Animation;
using Avalonia.Controls;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class CarouselPage : UserControl
    {
        private static readonly string[] TransitionNames = ["无", "PageSlide", "CrossFade"];

        public CarouselPage()
        {
            InitializeComponent();

            PreviousButton.Click += (_, _) => Slides.Previous();
            NextButton.Click += (_, _) => Slides.Next();
            Slides.SelectionChanged += (_, _) => IndexText.Text = $"第 {Slides.SelectedIndex + 1} / {Slides.ItemCount} 张";

            TransitionBox.ItemsSource = TransitionNames;
            TransitionBox.SelectedIndex = 0;
            TransitionBox.SelectionChanged += (_, _) =>
            {
                var duration = TimeSpan.FromMilliseconds(300);
                Slides.PageTransition = TransitionBox.SelectedIndex switch
                {
                    1 => new PageSlide(duration),
                    2 => new CrossFade(duration),
                    _ => null,
                };
            };
            SwipeCheck.IsCheckedChanged += (_, _) => Slides.IsSwipeEnabled = SwipeCheck.IsChecked == true;
        }
    }
}
