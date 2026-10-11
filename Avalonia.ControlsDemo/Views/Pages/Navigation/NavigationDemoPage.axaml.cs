using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class NavigationDemoPage : UserControl
    {
        private int _pageNumber = 1;
        private int _pushed, _popped, _modalPushed, _modalPopped;

        public NavigationDemoPage()
        {
            InitializeComponent();
            Nav.Content = DemoPages.Make("首页", "这是栈底的根页。点「Push 新页」压入下一页。");

            TransitionBox.ItemsSource = new[] { "无", "PageSlide", "CrossFade" };
            // The theme only fills in the default transition once the page is on screen, so sync the box then.
            Loaded += OnFirstLoaded;
            Nav.Pushed += (_, _) => { _pushed++; Refresh(); };
            Nav.Popped += (_, _) => { _popped++; Refresh(); };
            Nav.ModalPushed += (_, _) => { _modalPushed++; Refresh(); };
            Nav.ModalPopped += (_, _) => { _modalPopped++; Refresh(); };

            PushButton.Click += async (_, _) =>
            {
                _pageNumber++;
                await Nav.PushAsync(DemoPages.Make($"第 {_pageNumber} 页", $"这是第 {_pageNumber} 页，用左上角的返回按钮或「Pop」回去。"));
            };
            PopButton.Click += async (_, _) => await Nav.PopAsync();
            RootButton.Click += async (_, _) => await Nav.PopToRootAsync();
            ModalButton.Click += async (_, _) => await Nav.PushModalAsync(DemoPages.Make("模态页", "模态页盖在整个导航栈上面，用「PopModal」关掉。"));
            CloseModalButton.Click += async (_, _) => await Nav.PopModalAsync();

            Refresh();
        }

        private void OnFirstLoaded(object? sender, RoutedEventArgs e)
        {
            Loaded -= OnFirstLoaded;
            TransitionBox.SelectedIndex = Nav.PageTransition switch
            {
                PageSlide => 1,
                CrossFade => 2,
                _ => 0
            };
            TransitionBox.SelectionChanged += (_, _) => ApplyTransition();
        }

        private void ApplyTransition()
        {
            IPageTransition? transition = TransitionBox.SelectedItem switch
            {
                "PageSlide" => new PageSlide(TimeSpan.FromMilliseconds(250)),
                "CrossFade" => new CrossFade(TimeSpan.FromMilliseconds(250)),
                _ => null
            };
            Nav.PageTransition = transition;
            Nav.ModalTransition = transition;
        }

        private void Refresh()
        {
            StateText.Text = $"StackDepth = {Nav.StackDepth}，CanGoBack = {Nav.CanGoBack}，ModalStack = {Nav.ModalStack.Count}";
            EventText.Text = $"Pushed {_pushed} 次，Popped {_popped} 次，ModalPushed {_modalPushed} 次，ModalPopped {_modalPopped} 次";
        }
    }
}
