namespace Avalonia.DataBindingDemo.Models
{
    /// <summary>
    /// A row that is not a contact. Grouping is done by flattening groups into one
    /// list of headers and contacts; a DataTemplate per type renders each kind.
    /// </summary>
    public sealed record ContactGroupHeader(string Key, int Count);
}
