using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.DataTemplatesDemo.Models;
using Avalonia.Layout;
using Avalonia.Media;

namespace Avalonia.DataTemplatesDemo.Views.Pages
{
    public partial class CodeTemplatesPage : UserControl
    {
        public CodeTemplatesPage()
        {
            InitializeComponent();

            Circle[] circles =
            [
                new() { Name = "小", Radius = 12 },
                new() { Name = "中", Radius = 24 },
                new() { Name = "大", Radius = 36 },
            ];

            // The second lambda argument is the name scope; unused here.
            CodeList.ItemTemplate = new FuncDataTemplate<Circle>((circle, _) => new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children =
                {
                    new Ellipse
                    {
                        Fill = Brushes.Orange,
                        // Indexer syntax creates a binding against the item, like {Binding Radius}.
                        [!WidthProperty] = new Binding(nameof(Circle.Radius)),
                        [!HeightProperty] = new Binding(nameof(Circle.Radius)),
                    },
                    new TextBlock
                    {
                        VerticalAlignment = VerticalAlignment.Center,
                        [!TextBlock.TextProperty] = new Binding(nameof(Circle.Name)) { StringFormat = "代码模板：{0}" },
                    },
                },
            });
            CodeList.ItemsSource = circles;

            // A match predicate turns the template into a filter; the collection keeps looking on false.
            FilteredList.DataTemplates.Add(new FuncDataTemplate<Circle>(
                c => c.Radius > 20,
                (_, _) => new TextBlock
                {
                    Foreground = Brushes.Orange,
                    [!TextBlock.TextProperty] = new Binding(nameof(Circle.Name)) { StringFormat = "大圆模板：{0}" },
                }));
            FilteredList.DataTemplates.Add(new FuncDataTemplate<Circle>((_, _) => new TextBlock
            {
                Opacity = 0.6,
                [!TextBlock.TextProperty] = new Binding(nameof(Circle.Name)) { StringFormat = "兜底模板：{0}" },
            }));
            FilteredList.ItemsSource = circles;
        }
    }
}
