using Avalonia.Controls;
using System;

namespace Avalonia.ControlsDemo.Navigation
{
    /// <summary>Metadata of one demo page. <see cref="PageCatalog"/> is the only place these are created.</summary>
    /// <param name="Title">Plain control or topic name; the tree adds the "实战：" / "↗ " marks.</param>
    /// <param name="Category">One of the <see cref="Categories"/> constants.</param>
    /// <param name="DocPath">Official docs path without domain or leading slash, e.g. controls/input/buttons/button.</param>
    /// <param name="PageType">A <see cref="Control"/> subclass with a public parameterless constructor.</param>
    public sealed record PageEntry(string Title, string Category, string DocPath, Type PageType, PageKind Kind)
    {
        public Control CreatePage() => (Control)Activator.CreateInstance(PageType)!;
    }
}
