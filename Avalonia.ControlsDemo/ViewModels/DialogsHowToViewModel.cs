using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace Avalonia.ControlsDemo.ViewModels
{
    public partial class DialogsHowToViewModel : ObservableObject
    {
        private readonly IDialogService _dialogs;

        public DialogsHowToViewModel(IDialogService dialogs)
        {
            _dialogs = dialogs;
        }

        [ObservableProperty]
        private string _confirmResult = "（还没问过）";

        [ObservableProperty]
        private string _nameResult = "（还没问过）";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PickedBrush))]
        private Color _pickedColor = Colors.Gray;

        [ObservableProperty]
        private string _colorResult = "（还没选过）";

        public IBrush PickedBrush => new SolidColorBrush(PickedColor);

        [RelayCommand]
        private async Task AskConfirmAsync()
        {
            bool? answer = await _dialogs.ConfirmAsync("确认", "要删除这个项目吗？");
            ConfirmResult = answer switch
            {
                true => "结果 true：用户点了「确定」",
                false => "结果 false：用户点了「取消」",
                _ => "结果 null：用户直接关掉了窗口，既没确定也没取消"
            };
        }

        [RelayCommand]
        private async Task AskNameAsync()
        {
            string? name = await _dialogs.PromptAsync("新建项目", "给它起个名字：", required: false);
            NameResult = name is null ? "没有名字（取消或直接关闭）" : $"名字：{name}";
        }

        [RelayCommand]
        private async Task AskRequiredNameAsync()
        {
            string? name = await _dialogs.PromptAsync("必须回答", "这个问题不答不能关：", required: true);
            NameResult = name is null ? "没有名字" : $"名字（必答）：{name}";
        }

        [RelayCommand]
        private async Task PickColorAsync()
        {
            Color? picked = await _dialogs.PickColorAsync(PickedColor);
            if (picked is Color color)
            {
                PickedColor = color;
                ColorResult = $"结果 Color：{color}";
            }
            else
            {
                ColorResult = "结果 null：没选，颜色保持不变";
            }
        }
    }
}
