using Avalonia.Controls;
using Avalonia.DataTemplatesDemo.Models;

namespace Avalonia.DataTemplatesDemo.Views.Pages
{
    public partial class ReusePage : UserControl
    {
        public ReusePage()
        {
            InitializeComponent();

            Circle[] circles = [new() { Name = "甲" }, new() { Name = "乙" }];
            ReuseContent.Content = circles[0];
            ReuseList.ItemsSource = circles;
            ReuseCombo.ItemsSource = circles;
            ReuseCombo.SelectedIndex = 0;

            ImplicitA.Content = new Square { Name = "方 A", Side = 4 };
            ImplicitB.Content = new Square { Name = "方 B", Side = 6 };
        }
    }
}
