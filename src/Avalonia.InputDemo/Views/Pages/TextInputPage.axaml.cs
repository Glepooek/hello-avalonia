using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using System.Linq;

namespace Avalonia.InputDemo.Views.Pages
{
    public partial class TextInputPage : UserControl
    {
        public TextInputPage()
        {
            InitializeComponent();

            // Tunnel: this runs before the TextBox's own handling, so Handled really stops the character.
            DigitsBox.AddHandler(InputElement.TextInputEvent, (object? s, TextInputEventArgs e) =>
            {
                if (e.Text is { Length: > 0 } text && !text.All(char.IsDigit))
                {
                    e.Handled = true;
                }
            }, RoutingStrategies.Tunnel);

            // Read the attached values back off the first two boxes so the readout proves they were applied.
            var email = (TextBox)((StackPanel)OptionsBox.Parent!).Children[0];
            var digits = (TextBox)((StackPanel)OptionsBox.Parent!).Children[1];
            OptionsReadout.Text =
                $"邮箱框  ContentType={TextInputOptions.GetContentType(email)}  ReturnKeyType={TextInputOptions.GetReturnKeyType(email)}\n" +
                $"数字框  ContentType={TextInputOptions.GetContentType(digits)}\n" +
                $"密码框  IsSensitive={TextInputOptions.GetIsSensitive(OptionsBox)}";
        }
    }
}
