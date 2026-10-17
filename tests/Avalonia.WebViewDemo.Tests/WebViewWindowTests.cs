using Avalonia.Headless.XUnit;
using Avalonia.Shared.Messages;
using Avalonia.Threading;
using Avalonia.WebViewDemo.Views;
using CommunityToolkit.Mvvm.Messaging;
using Xunit;

namespace Avalonia.WebViewDemo.Tests
{
    // Constructed only, never shown: see SmokeTests for why WebView2 cannot start under headless.
    public class WebViewWindowTests
    {
        [AvaloniaFact]
        public void Window_listens_for_js_calls_until_it_is_closed()
        {
            var window = new WebViewWindow();
            Assert.True(WeakReferenceMessenger.Default.IsRegistered<MessageParam>(window));

            window.Close();
            Dispatcher.UIThread.RunJobs();

            Assert.False(WeakReferenceMessenger.Default.IsRegistered<MessageParam>(window));
        }

        // Button #3 can be clicked while the page is still loading; the async void handler must not crash the app.
        [AvaloniaFact]
        public void Call_js_before_the_page_has_loaded_does_not_throw()
        {
            var window = new WebViewWindow();
            try
            {
                var error = Record.Exception(() =>
                {
                    WeakReferenceMessenger.Default.Send(new MessageParam { Reult = true });
                    Dispatcher.UIThread.RunJobs();
                });

                Assert.Null(error);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Messages_that_are_not_js_calls_are_ignored()
        {
            var window = new WebViewWindow();
            try
            {
                var error = Record.Exception(() =>
                {
                    WeakReferenceMessenger.Default.Send(new MessageParam { Reult = false });
                    Dispatcher.UIThread.RunJobs();
                });

                Assert.Null(error);
            }
            finally
            {
                window.Close();
            }
        }
    }
}
