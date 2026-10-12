using Avalonia.Controls;
using Avalonia.DataTemplatesDemo.Models;

namespace Avalonia.DataTemplatesDemo.Views.Pages
{
    public partial class SelectorPage : UserControl
    {
        public SelectorPage()
        {
            InitializeComponent();

            // Same two people as the old DataTemplateDemo, plus one more so each template shows twice or more.
            PeopleList.ItemsSource = new[]
            {
                new Person { Id = 1, Name = "anyu", Address = "Beijing", Sex = Sex.Male },
                new Person { Id = 2, Name = "lff", Address = "Beijing", Sex = Sex.Female },
                new Person { Id = 3, Name = "小明", Address = "Shanghai", Sex = Sex.Male },
            };
        }
    }
}
