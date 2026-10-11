using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class NavigationHowToPage : UserControl
    {
        public NavigationHowToPage()
        {
            InitializeComponent();
            DataContext = new NavigationHowToViewModel();

            TransitionBox.ItemsSource = new[] { "无", "CrossFade", "PageSlide" };
            TransitionBox.SelectedIndex = Host.PageTransition switch
            {
                CrossFade => 1,
                PageSlide => 2,
                _ => 0
            };
            TransitionBox.SelectionChanged += (_, _) =>
            {
                Host.PageTransition = TransitionBox.SelectedItem switch
                {
                    "CrossFade" => new CrossFade(TimeSpan.FromMilliseconds(250)),
                    "PageSlide" => new PageSlide(TimeSpan.FromMilliseconds(250)),
                    _ => null
                };
            };
        }
    }
}
