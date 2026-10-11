using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class MaskedTextBoxPage : UserControl
    {
        public MaskedTextBoxPage()
        {
            InitializeComponent();

            Phone.PropertyChanged += (_, e) =>
            {
                if (e.Property == TextBox.TextProperty)
                {
                    PhoneResult.Text = Phone.MaskCompleted == true ? $"已填完：{Phone.Text}" : "未填完";
                }
            };
        }
    }
}
