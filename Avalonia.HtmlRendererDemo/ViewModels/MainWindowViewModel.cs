using Avalonia.Shared.ViewModels;

namespace Avalonia.HtmlRendererDemo.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public string Text { get; }

        public MainWindowViewModel()
        {
            Text = " <h1>欢迎来到我的网页</h1>\r\n    <p>这是一个段落。你可以在这里添加内容。</p>\r\n    <p>学习HTML是创建网页的第一步。</p>\r\n";
        }
    }
}
