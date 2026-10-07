using CommunityToolkit.Mvvm.ComponentModel;

namespace Avalonia.DataBindingDemo.Models
{
    /// <summary>
    /// Observable so that editing a contact in a detail pane updates every
    /// list showing it — the list items and the detail bind to the same object.
    /// </summary>
    public partial class Contact : ObservableObject
    {
        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private string _city;

        [ObservableProperty]
        private int _age;

        public Contact(string name, string city, int age)
        {
            _name = name;
            _city = city;
            _age = age;
        }

        // ListBox falls back to ToString when no template is given; make that readable.
        public override string ToString() => $"{Name}（{City}）";

        public static Contact[] Samples() =>
        [
            new("张三", "北京", 28),
            new("李四", "上海", 35),
            new("王五", "北京", 22),
            new("赵六", "广州", 41),
            new("钱七", "上海", 30),
            new("孙八", "深圳", 26),
        ];
    }
}
