using Avalonia.Controls;
using System.Linq;

namespace Avalonia.XamlDemo.Views.Pages
{
    public partial class CompilationPage : UserControl
    {
        public CompilationPage()
        {
            InitializeComponent();

            // Compiled XAML turns each .axaml into generated code in this same
            // assembly, so the populate methods show up as compiler-generated types.
            var assembly = typeof(CompilationPage).Assembly;
            var xamlTypes = assembly.GetTypes()
                .Count(t => t.Namespace?.StartsWith("CompiledAvaloniaXaml") == true);

            CompiledFlagText.Text =
                $"本程序集：{assembly.GetName().Name}，"
                + $"编译期生成的 XAML 类型 {xamlTypes} 个。"
                + "若关闭编译型 XAML，这些类型不会存在，界面改由运行时解析器现场构建。";
        }
    }
}
