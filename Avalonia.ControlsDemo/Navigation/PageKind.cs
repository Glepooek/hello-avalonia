namespace Avalonia.ControlsDemo.Navigation
{
    /// <summary>How a page relates to the official documentation, and how it is marked in the tree.</summary>
    public enum PageKind
    {
        /// <summary>A runnable demonstration of one control.</summary>
        Control,

        /// <summary>A hands-on page for one official How-to guide, placed next to its control.</summary>
        HowTo,

        /// <summary>No code: a note pointing at another project or at the official docs.</summary>
        Signpost,
    }
}
