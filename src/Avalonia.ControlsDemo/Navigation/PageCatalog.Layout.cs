using Avalonia.ControlsDemo.Views.Pages;
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.Navigation
{
    public static partial class PageCatalog
    {
        private static void AddLayout(List<PageEntry> list)
        {
            const string c = Categories.Layout;

            // Containers, in the order of the official sidebar.
            list.Add(ControlPage<BorderPage>("Border", c, "controls/layout/containers/border"));
            list.Add(ControlPage<ExpanderPage>("Expander", c, "controls/layout/containers/expander"));
            list.Add(HowToPage<ExpanderHowToPage>("Expander", c, "docs/how-to/expander-how-to"));
            list.Add(ControlPage<FlyoutPage>("Flyout", c, "controls/layout/containers/flyout"));
            list.Add(ControlPage<GroupBoxPage>("GroupBox", c, "controls/layout/containers/groupbox"));
            list.Add(ControlPage<PipsPagerPage>("PipsPager", c, "controls/layout/containers/pipspager"));
            list.Add(ControlPage<RefreshContainerPage>("RefreshContainer", c, "controls/layout/containers/refreshcontainer"));
            list.Add(ControlPage<ScrollViewerPage>("ScrollViewer", c, "controls/layout/containers/scrollviewer"));
            list.Add(HowToPage<ScrollViewerHowToPage>("ScrollViewer", c, "docs/how-to/scrollviewer-how-to"));
            list.Add(ControlPage<SplitViewPage>("SplitView", c, "controls/layout/containers/splitview"));
            list.Add(ControlPage<ViewboxPage>("Viewbox", c, "controls/layout/containers/viewbox"));

            // Decorator
            list.Add(ControlPage<DecoratorPage>("Decorator", c, "controls/layout/decorator"));

            // LayoutTransformControl
            list.Add(ControlPage<LayoutTransformControlPage>("LayoutTransformControl", c, "controls/layout/layouttransformcontrol"));

            // Panels
            list.Add(ControlPage<CanvasPage>("Canvas", c, "controls/layout/panels/canvas"));
            list.Add(ControlPage<DockPanelPage>("DockPanel", c, "controls/layout/panels/dockpanel"));
            list.Add(ControlPage<GridPage>("Grid", c, "controls/layout/panels/grid"));
            list.Add(HowToPage<GridHowToPage>("Grid", c, "docs/how-to/grid-how-to"));
            list.Add(ControlPage<GridSplitterPage>("GridSplitter", c, "controls/layout/panels/gridsplitter"));
            list.Add(ControlPage<PanelPage>("Panel", c, "controls/layout/panels/panel"));
            list.Add(ControlPage<RelativePanelPage>("RelativePanel", c, "controls/layout/panels/relativepanel"));
            list.Add(ControlPage<StackPanelPage>("StackPanel", c, "controls/layout/panels/stackpanel"));
            list.Add(ControlPage<UniformGridPage>("UniformGrid", c, "controls/layout/panels/uniformgrid"));
            list.Add(ControlPage<WrapPanelPage>("WrapPanel", c, "controls/layout/panels/wrappanel"));
        }
    }
}
