using Avalonia.Controls;
using Avalonia.Controls.Templates;
using System;

namespace Avalonia.DataTemplatesDemo
{
    /// <summary>
    /// Convention-based lookup: Avalonia.DataTemplatesDemo.ViewModels.FooViewModel resolves to
    /// Avalonia.DataTemplatesDemo.Views.Located.FooView. This is the same idea as the ViewLocator
    /// the Avalonia MVVM template generates, narrowed to one folder so it cannot accidentally
    /// claim the page view models used elsewhere.
    /// </summary>
    public class ViewLocator : IDataTemplate
    {
        public Control? Build(object? data)
        {
            var name = data!.GetType().FullName!
                .Replace(".ViewModels.", ".Views.Located.")
                .Replace("ViewModel", "View");
            var type = Type.GetType(name);

            // A visible miss is better than a blank area: the user sees which name was tried.
            return type is null
                ? new TextBlock { Text = $"找不到视图：{name}" }
                : (Control)Activator.CreateInstance(type)!;
        }

        public bool Match(object? data) => data?.GetType().Name.EndsWith("ViewModel") == true;
    }
}
