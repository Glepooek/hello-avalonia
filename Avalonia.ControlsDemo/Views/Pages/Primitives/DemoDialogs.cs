using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System.Threading.Tasks;

namespace Avalonia.ControlsDemo.Views.Pages
{
    /// <summary>Builds the small modal dialogs used by the Window and Dialogs how-to pages.</summary>
    internal static class DemoDialogs
    {
        public static Task<bool?> ConfirmAsync(Window owner, string title, string message)
        {
            var ok = new Button { Name = "OkButton", Content = "确定", MinWidth = 72 };
            var cancel = new Button { Name = "CancelButton", Content = "取消", MinWidth = 72 };
            var dialog = Create(title, new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, MaxWidth = 320 }, ok, cancel);

            ok.Click += (_, _) => dialog.Close(true);
            cancel.Click += (_, _) => dialog.Close(false);
            return dialog.ShowDialog<bool?>(owner);
        }

        /// <summary>
        /// Asks for a line of text. With <paramref name="required"/> the title bar's close button is held back
        /// (and Cancel is disabled) until the user has answered.
        /// </summary>
        public static Task<string?> PromptAsync(Window owner, string title, string prompt, bool required)
        {
            var box = new TextBox { Name = "AnswerBox", PlaceholderText = "在这里输入" };
            var ok = new Button { Name = "OkButton", Content = "确定", MinWidth = 72, IsEnabled = false };
            var cancel = new Button { Name = "CancelButton", Content = "取消", MinWidth = 72, IsEnabled = !required };
            var body = new StackPanel { Spacing = 8, Children = { new TextBlock { Text = prompt }, box } };
            var dialog = Create(title, body, ok, cancel);

            bool answered = false;
            box.TextChanged += (_, _) => ok.IsEnabled = !string.IsNullOrWhiteSpace(box.Text);
            ok.Click += (_, _) =>
            {
                answered = true;
                dialog.Close(box.Text!.Trim());
            };
            cancel.Click += (_, _) =>
            {
                answered = true;
                dialog.Close(null);
            };
            if (required)
            {
                dialog.Closing += (_, e) =>
                {
                    if (!answered && e.CloseReason == WindowCloseReason.WindowClosing) e.Cancel = true;
                };
            }
            return dialog.ShowDialog<string?>(owner);
        }

        /// <summary>A "complex result" dialog: the answer is a Color, not a bool.</summary>
        public static Task<Color?> PickColorAsync(Window owner, Color initial)
        {
            Window? dialog = null;
            var swatches = new WrapPanel { Width = 240 };
            foreach (var (name, color) in new[]
            {
                ("Red", Colors.Crimson), ("Green", Colors.SeaGreen), ("Blue", Colors.SteelBlue), ("Orange", Colors.Orange)
            })
            {
                var swatch = new Button
                {
                    Name = "Swatch_" + name,
                    Margin = new Thickness(0, 0, 8, 8),
                    // The colour sits in the content so the button keeps its theme hover and pressed states.
                    Content = new Border { Width = 40, Height = 24, Background = new SolidColorBrush(color) }
                };
                swatch.Click += (_, _) => dialog!.Close(color);
                swatches.Children.Add(swatch);
            }

            var cancel = new Button { Name = "CancelButton", Content = "取消", MinWidth = 72 };
            cancel.Click += (_, _) => dialog!.Close(null);
            var body = new StackPanel { Spacing = 8, Children = { new TextBlock { Text = $"当前颜色：{initial}" }, swatches } };
            dialog = Create("选一个颜色", body, cancel);
            return dialog.ShowDialog<Color?>(owner);
        }

        private static Window Create(string title, Control body, params Button[] buttons)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
            foreach (var button in buttons) row.Children.Add(button);

            return new Window
            {
                Title = title,
                SizeToContent = SizeToContent.WidthAndHeight,
                MinWidth = 280,
                CanResize = false,
                CanMinimize = false,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel { Margin = new Thickness(16), Spacing = 12, Children = { body, row } }
            };
        }
    }
}
