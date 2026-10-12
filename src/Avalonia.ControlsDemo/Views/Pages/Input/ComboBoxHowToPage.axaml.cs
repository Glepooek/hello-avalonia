using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;
using System;
using System.Linq;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ComboBoxHowToPage : UserControl
    {
        private static readonly Employee[] Staff =
        {
            new("张伟", "研发部"),
            new("王芳", "市场部"),
            new("李娜", "财务部"),
            new("刘洋", "研发部"),
            new("陈静", "人事部"),
        };

        public ComboBoxHowToPage()
        {
            InitializeComponent();

            EmployeeBox.ItemsSource = Staff;

            PriorityBox.ItemsSource = PriorityOption.All;
            PriorityBox.SelectionChanged += (_, _) =>
                PriorityResult.Text = $"SelectedValue = {PriorityBox.SelectedValue ?? "(空)"}";

            // AutoCompleteBox filters on the string produced by ItemSelector, so give it the name.
            SearchEmployee.ItemsSource = Staff;
            SearchEmployee.ItemSelector = (_, item) => (item as Employee)?.Name ?? string.Empty;
            SearchEmployee.ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<Employee>(
                (e, _) => new TextBlock { Text = $"{e.Name}（{e.Department}）" });
            SearchEmployee.SelectionChanged += (_, _) =>
                SearchResult.Text = SearchEmployee.SelectedItem is Employee e ? $"已选：{e.Name}，{e.Department}" : "未选择";
        }
    }
}
