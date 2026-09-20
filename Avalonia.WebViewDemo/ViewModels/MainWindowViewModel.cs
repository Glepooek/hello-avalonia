using Avalonia.Controls;
using Avalonia.Shared.Helpers;
using Avalonia.Shared.Messages;
using Avalonia.Shared.ViewModels;
using Avalonia.WebViewDemo.Views;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Diagnostics;

namespace Avalonia.WebViewDemo.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        #region Commands

        public RelayCommand ShowWebViewCommand { get; private set; }
        public RelayCommand ShowWebDialogCommand { get; private set; }
        public RelayCommand CallJSMethodCommand { get; private set; }

        #endregion

        #region Constructor

        public MainWindowViewModel()
        {
            // NativeWebView hosted inside an Avalonia Window.
            ShowWebViewCommand = new RelayCommand(() =>
            {
                WebViewWindow dialog = new WebViewWindow();
                dialog.Show(AvaloniaHelper.GetMainWindow());
            });

            // NativeWebDialog hosts web content in its own native window, with no Avalonia Window wrapper.
            ShowWebDialogCommand = new RelayCommand(ShowNativeWebDialog);

            // C# -> JS, received by the open WebViewWindow.
            CallJSMethodCommand = new RelayCommand(() =>
            {
                WeakReferenceMessenger.Default.Send<MessageParam>(new MessageParam { Reult = true });
            });
        }

        #endregion

        #region Methods

        /// <summary>
        /// Demonstrates NativeWebDialog: a native window hosting web content, shown owned by the main window.
        /// </summary>
        private void ShowNativeWebDialog()
        {
            var dialog = new NativeWebDialog
            {
                Title = "NativeWebDialog Demo",
                CanUserResize = true,
                Source = new Uri("https://avaloniaui.net/"),
            };

            dialog.NavigationCompleted += async (s, e) =>
            {
                if (!e.IsSuccess)
                {
                    return;
                }

                var title = await dialog.InvokeScript("document.title");
                Debug.WriteLine($"NativeWebDialog loaded, document.title = {title}");
            };

            dialog.Closing += (s, e) => Debug.WriteLine("NativeWebDialog closing");

            var owner = AvaloniaHelper.GetMainWindow();
            if (owner is not null)
            {
                dialog.Show(owner);
            }
            else
            {
                dialog.Show();
            }

            dialog.Resize(1000, 700);
        }

        #endregion
    }
}
