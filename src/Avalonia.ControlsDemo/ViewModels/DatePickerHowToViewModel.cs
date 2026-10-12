using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Avalonia.ControlsDemo.ViewModels
{
    /// <summary>Backs the DatePicker how-to: a date that may not be in the past, plus a time, combined into one value.</summary>
    public sealed partial class DatePickerHowToViewModel : ObservableValidator
    {
        public DatePickerHowToViewModel()
        {
            ErrorsChanged += (_, _) => OnPropertyChanged(nameof(ErrorText));
        }

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [NotifyPropertyChangedFor(nameof(Combined))]
        [CustomValidation(typeof(DatePickerHowToViewModel), nameof(ValidateNotPast))]
        private DateTimeOffset? _date;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Combined))]
        private TimeSpan? _time;

        public string Combined => Date is { } date && Time is { } time
            ? $"预约时间：{date.Date + time:yyyy-MM-dd HH:mm}"
            : "请同时选择日期和时间";

        public string ErrorText => HasErrors
            ? string.Join("；", GetErrors(nameof(Date)).Select(e => e.ErrorMessage))
            : string.Empty;

        public static ValidationResult? ValidateNotPast(DateTimeOffset? value, ValidationContext context)
            => value is { } date && date.Date < DateTime.Today
                ? new ValidationResult("日期不能早于今天")
                : ValidationResult.Success;
    }
}
