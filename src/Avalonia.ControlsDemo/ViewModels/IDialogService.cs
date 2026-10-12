using Avalonia.Media;
using System.Threading.Tasks;

namespace Avalonia.ControlsDemo.ViewModels
{
    /// <summary>What a view model may ask of the UI: show a dialog and await the user's answer.</summary>
    public interface IDialogService
    {
        /// <summary>True for OK, false for Cancel, null when the user simply closed the dialog.</summary>
        Task<bool?> ConfirmAsync(string title, string message);

        /// <summary>The trimmed text the user typed, or null when they backed out.</summary>
        Task<string?> PromptAsync(string title, string prompt, bool required);

        /// <summary>The chosen colour, or null when they cancelled.</summary>
        Task<Color?> PickColorAsync(Color initial);
    }
}
