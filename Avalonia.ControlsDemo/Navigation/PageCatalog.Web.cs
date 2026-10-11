using Avalonia.ControlsDemo.Views.Pages;
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.Navigation
{
    public static partial class PageCatalog
    {
        private static void AddWeb(List<PageEntry> list)
        {
            const string c = Categories.Web;

            // No runnable demo lives in this project: Avalonia.WebViewDemo covers two of the four.
            list.Add(SignpostPage<NativeWebViewSignpostPage>("NativeWebView", c, "controls/web/nativewebview"));
            list.Add(SignpostPage<NativeWebDialogSignpostPage>("NativeWebDialog", c, "controls/web/nativewebdialog"));
            list.Add(SignpostPage<WebAuthenticationBrokerSignpostPage>("WebAuthenticationBroker", c, "controls/web/webauthenticationbroker"));
            list.Add(SignpostPage<WebViewEnvironmentSignpostPage>("WebView 环境配置", c, "controls/web/webview-environment"));
        }
    }
}
