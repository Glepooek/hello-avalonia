using Avalonia.Controls;
using Avalonia.AppDevelopmentDemo.ViewModels;

namespace Avalonia.AppDevelopmentDemo.Views.Pages
{
    public partial class XamlDesignTimePage : UserControl
    {
        public XamlDesignTimePage()
        {
            InitializeComponent();
            if (!Design.IsDesignMode)
                DataContext = DesignTimeSampleViewModel.CreateRuntime();
        }
    }
}
