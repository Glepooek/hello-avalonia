using Avalonia.Headless.XUnit;
using Avalonia.Shared.Messages;
using Avalonia.WebViewDemo.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.Generic;
using Xunit;

namespace Avalonia.WebViewDemo.Tests
{
    public class ViewModelTests
    {
        // Not private: the toolkit's RegisterAll source generator emits code that must see the type.
        internal sealed class Recipient : IRecipient<MessageParam>
        {
            public List<MessageParam> Received { get; } = new();
            public void Receive(MessageParam message) => Received.Add(message);
        }

        [AvaloniaFact]
        public void Call_js_command_broadcasts_a_request_to_open_web_view_windows()
        {
            var recipient = new Recipient();
            WeakReferenceMessenger.Default.Register(recipient);
            try
            {
                new MainWindowViewModel().CallJSMethodCommand.Execute(null);

                Assert.True(Assert.Single(recipient.Received).Reult);
            }
            finally
            {
                WeakReferenceMessenger.Default.UnregisterAll(recipient);
            }
        }
    }
}
