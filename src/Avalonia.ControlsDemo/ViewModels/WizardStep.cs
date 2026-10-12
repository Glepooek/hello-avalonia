using System;

namespace Avalonia.ControlsDemo.ViewModels
{
    /// <summary>A wizard step whose content is only built the first time it is shown.</summary>
    public class WizardStep
    {
        private readonly Func<string> _factory;
        private string? _body;

        public WizardStep(string title, Func<string> factory)
        {
            Title = title;
            _factory = factory;
        }

        public string Title { get; }

        public string Body => _body ??= _factory();
    }
}
