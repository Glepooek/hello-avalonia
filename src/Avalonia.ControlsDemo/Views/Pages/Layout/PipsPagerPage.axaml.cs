using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class PipsPagerPage : UserControl
    {
        public PipsPagerPage()
        {
            InitializeComponent();

            Pager.SelectedIndexChanged += (_, _) => PageText.Text = $"第 {Pager.SelectedPageIndex + 1} 页";
            PagesSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    Pager.NumberOfPages = (int)PagesSlider.Value;
                    Pager.MaxVisiblePips = (int)PagesSlider.Value;
                }
            };
            PreviousCheck.IsCheckedChanged += (_, _) => Pager.IsPreviousButtonVisible = PreviousCheck.IsChecked == true;
            NextCheck.IsCheckedChanged += (_, _) => Pager.IsNextButtonVisible = NextCheck.IsChecked == true;
        }
    }
}
