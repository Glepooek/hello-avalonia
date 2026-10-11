using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class UniformGridPage : UserControl
    {
        public UniformGridPage()
        {
            InitializeComponent();

            // Columns and FirstColumn are ints; the handlers convert the slider's double.
            ColumnsSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    Uniform.Columns = (int)ColumnsSlider.Value;
                }
            };
            FirstSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    Uniform.FirstColumn = (int)FirstSlider.Value;
                }
            };
        }
    }
}
