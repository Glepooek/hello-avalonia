using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using System.Linq;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class CheckBoxPage : UserControl
    {
        private bool _updating;

        public CheckBoxPage()
        {
            InitializeComponent();

            foreach (var child in new[] { ChildA, ChildB, ChildC })
            {
                child.IsCheckedChanged += (_, _) => SummariseChildren();
            }

            ParentBox.IsCheckedChanged += (_, _) =>
            {
                // The guard stops the summary below from fighting the user's click on the parent.
                if (_updating || ParentBox.IsChecked is null)
                {
                    return;
                }

                _updating = true;
                foreach (var child in new[] { ChildA, ChildB, ChildC })
                {
                    child.IsChecked = ParentBox.IsChecked;
                }
                _updating = false;
                ThreeResult.Text = $"父项：{(ParentBox.IsChecked == true ? "全选" : "未选")}";
            };
        }

        private void SummariseChildren()
        {
            if (_updating)
            {
                return;
            }

            var checkedCount = new[] { ChildA, ChildB, ChildC }.Count(c => c.IsChecked == true);
            _updating = true;
            ParentBox.IsChecked = checkedCount switch
            {
                0 => false,
                3 => true,
                _ => null,
            };
            _updating = false;
            ThreeResult.Text = $"父项：{(ParentBox.IsChecked is null ? "部分选中" : ParentBox.IsChecked == true ? "全选" : "未选")}";
        }
    }
}
