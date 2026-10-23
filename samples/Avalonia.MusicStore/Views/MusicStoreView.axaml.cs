using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;

namespace Avalonia.MusicStore.Views;

public partial class MusicStoreView : UserControl
{
    public MusicStoreView()
    {
        InitializeComponent();

        //// 属性更改，触发的方法订阅
        //var repeatCount = this.GetObservable(MusicStoreView.RepeatCountProperty);
        //repeatCount.Subscribe(OnRepeatCountPropertyChanged);
    }

    //static MusicStoreView()
    //{
    //    // 属性更改，触发的方法订阅
    //    RepeatCountProperty.Changed.AddClassHandler<MusicStoreView>(OnRepeatCountPropertyChanged1);
    //}

    //private static void OnRepeatCountPropertyChanged1(MusicStoreView view, AvaloniaPropertyChangedEventArgs args)
    //{

    //}

    //private void OnRepeatCountPropertyChanged(int obj)
    //{

    //}

    #region 样式化属性、附加属性

    public static readonly StyledProperty<int> RepeatCountProperty =
        AvaloniaProperty.Register<MusicStoreView, int>(nameof(RepeatCount), defaultValue: 1);

    public int RepeatCount
    {
        get => GetValue(RepeatCountProperty);
        set => SetValue(RepeatCountProperty, value);
    }

    #endregion

    #region 路由事件定义及引发

    public static readonly RoutedEvent<RoutedEventArgs> ValueChangedEvent =
        RoutedEvent.Register<MusicStoreView, RoutedEventArgs>(nameof(ValueChanged), RoutingStrategies.Bubble);

    public event EventHandler<RoutedEventArgs> ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    private void RaiseEvent()
    {
        RoutedEventArgs args = new RoutedEventArgs(ValueChangedEvent, this);
        RaiseEvent(args);
    }

    #endregion 
}