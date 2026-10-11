using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;
using System;
using System.Linq;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class TextBoxHowToPage : UserControl
    {
        private static readonly string[] Fruits = { "苹果", "香蕉", "樱桃", "葡萄", "橙子", "西瓜", "草莓" };

        public TextBoxHowToPage()
        {
            InitializeComponent();
            DataContext = new TextBoxHowToViewModel();

            // Digits only: rewriting Text is the only filter available in 12.1.2.
            // Setting Text raises TextChanging again, so only write when something was removed.
            DigitsBox.TextChanging += (_, _) =>
            {
                var text = DigitsBox.Text ?? string.Empty;
                var digits = new string(text.Where(char.IsDigit).ToArray());
                if (digits != text)
                {
                    DigitsBox.Text = digits;
                    DigitsBox.CaretIndex = digits.Length;
                    DigitsResult.Text = "已滤掉非数字";
                }
                else
                {
                    DigitsResult.Text = string.Empty;
                }
            };

            SearchList.ItemsSource = Fruits;
            SearchBox.TextChanged += (_, _) =>
            {
                var key = SearchBox.Text ?? string.Empty;
                SearchList.ItemsSource = Fruits.Where(f => f.Contains(key, StringComparison.OrdinalIgnoreCase)).ToList();
            };
            ClearButton.Click += (_, _) => SearchBox.Clear();

            UndoButton.Click += (_, _) => UndoBox.Undo();
            RedoButton.Click += (_, _) => UndoBox.Redo();

            CutItem.Click += (_, _) => MenuBox.Cut();
            CopyItem.Click += (_, _) => MenuBox.Copy();
            PasteItem.Click += (_, _) => MenuBox.Paste();
        }
    }
}
