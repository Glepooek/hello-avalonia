using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;
using Avalonia.Media;
using System;
using System.Threading.Tasks;

namespace Avalonia.ControlsDemo.Views.Pages
{
    /// <summary>The real IDialogService: finds the owner window on demand and shows a DemoDialogs dialog over it.</summary>
    internal sealed class WindowDialogService : IDialogService
    {
        private readonly Func<Window?> _owner;

        public WindowDialogService(Func<Window?> owner)
        {
            _owner = owner;
        }

        public Task<bool?> ConfirmAsync(string title, string message) =>
            _owner() is { } owner ? DemoDialogs.ConfirmAsync(owner, title, message) : Task.FromResult<bool?>(null);

        public Task<string?> PromptAsync(string title, string prompt, bool required) =>
            _owner() is { } owner ? DemoDialogs.PromptAsync(owner, title, prompt, required) : Task.FromResult<string?>(null);

        public Task<Color?> PickColorAsync(Color initial) =>
            _owner() is { } owner ? DemoDialogs.PickColorAsync(owner, initial) : Task.FromResult<Color?>(null);
    }
}
