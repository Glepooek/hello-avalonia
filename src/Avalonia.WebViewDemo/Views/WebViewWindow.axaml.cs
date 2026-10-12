using Avalonia.Controls;
using Avalonia.Shared.Messages;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Avalonia.WebViewDemo.Views;

/// <summary>
/// Demonstrates the embedded <see cref="NativeWebView"/> control shipped with Avalonia 12
/// (WebView2 on Windows, WKWebView on macOS, WPE/WebKitGTK on Linux).
/// </summary>
public partial class WebViewWindow : Window, IRecipient<MessageParam>
{
    public WebViewWindow()
    {
        InitializeComponent();
        WeakReferenceMessenger.Default.Register<MessageParam>(this);
        Closed += OnClosed;

        var page = Path.Combine(AppContext.BaseDirectory, "TestWeb", "index.html");
        webView.Source = new Uri(page);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        WeakReferenceMessenger.Default.Unregister<MessageParam>(this);
    }

    /// <summary>
    /// Hides the loading spinner once the page is rendered.
    /// </summary>
    private void WebView_NavigationCompleted(object? sender, WebViewNavigationCompletedEventArgs e)
    {
        Dispatcher.UIThread.Post(() => canvas.IsVisible = false);
    }

    /// <summary>
    /// JS -> C#. The host injects a global invokeCSharpAction(body); the body is a JSON envelope
    /// { action, requestId, payload } so a reply can be correlated back to the awaiting JS promise.
    /// This replaces CefGlue's RegisterJavascriptObject host-object binding, which NativeWebView has no equivalent for.
    /// </summary>
    private async void WebView_WebMessageReceived(object? sender, WebMessageReceivedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.Body))
        {
            return;
        }

        string action;
        string? requestId;
        try
        {
            var envelope = JsonNode.Parse(e.Body)?.AsObject();
            action = envelope?["action"]?.GetValue<string>() ?? string.Empty;
            requestId = envelope?["requestId"]?.GetValue<string>();
        }
        catch (JsonException ex)
        {
            Debug.WriteLine($"Invalid web message: {ex.Message}");
            return;
        }

        var result = Dispatch(action);

        if (requestId is not null)
        {
            var json = JsonSerializer.Serialize(result);
            await webView.InvokeScript($"window.avaBridge.resolve({JsonSerializer.Serialize(requestId)}, {json})");
        }
    }

    /// <summary>
    /// Host methods callable from JavaScript. Replaces the CefGlue JSCallback host object.
    /// </summary>
    private object? Dispatch(string action)
    {
        switch (action)
        {
            case "closeWindow":
                Close();
                return null;

            case "minimizeWindow":
                WindowState = WindowState.Minimized;
                return null;

            case "maximizeWindow":
                WindowState = WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
                return null;

            case "getHttpHeaderParamsInfo":
                return new
                {
                    deviceName = Environment.MachineName,
                    deviceModel = Environment.OSVersion.VersionString,
                    appKey = "avalonia_musicstore_demo",
                };

            case "checkAppUpdate":
                return $"Already up to date ({DateTime.Now:yyyy-MM-dd HH:mm:ss})";

            default:
                Debug.WriteLine($"Unknown web action: {action}");
                return null;
        }
    }

    /// <summary>
    /// C# -> JS, triggered by the main window's "call JS method" button.
    /// </summary>
    public async void Receive(MessageParam message)
    {
        if (message?.Reult != true)
        {
            return;
        }

        var result = await webView.InvokeScript("window.calculateAdd(12, 13)");
        Debug.WriteLine($"JS returned: {result}");
    }
}
