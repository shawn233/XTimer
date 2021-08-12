using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.System.Threading;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace XTimer
{
    // A convenient timer class
    public class XCoreTimer
    {
        private int _total_seconds;
        private int seconds_left;
        private double _update_seconds;
        
        private ThreadPoolTimer core_timer;

        // UI-related
        private TextBlock _timer_minutes;
        private TextBlock _timer_seconds;
        private Windows.UI.Core.CoreDispatcher _core_dispatcher;

        public XCoreTimer(
            int total_seconds, TextBlock timer_minutes, TextBlock timer_seconds,
            Windows.UI.Core.CoreDispatcher core_dispatcher,
            double update_period_in_seconds=1.0)
        {
            seconds_left = _total_seconds = total_seconds;
            _update_seconds = update_period_in_seconds;

            _timer_minutes = timer_minutes;
            _timer_seconds = timer_seconds;
            _core_dispatcher = core_dispatcher;
        }

        public async void Run()
        {
            core_timer = ThreadPoolTimer.CreatePeriodicTimer(
                (source) => 
                {
                    seconds_left -= 1;

                    _core_dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High,
                        () =>
                        {
                            _timer_minutes.Text = (seconds_left / 60).ToString("D2");
                            _timer_seconds.Text = (seconds_left % 60).ToString("D2");
                        });

                    if (seconds_left == 0)
                    {
                        source.Cancel();
                    }
                }, 
                TimeSpan.FromSeconds(_update_seconds));
        }

        public void Cancel()
        {
            core_timer.Cancel();
        }

        public void Button_Cancel(object sender, RoutedEventArgs e)
        {
            Cancel();
        }
    }

    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private TimeSpan tick_period;
        private ThreadPoolTimer timer;
        private int timer_seconds;

        public MainPage()
        {
            this.InitializeComponent();

            tick_period = TimeSpan.FromSeconds(1.0);
            timer = null;

            RelativePanel tomato = new RelativePanel();
            tomato.Width = SplitView_TimerList.OpenPaneLength - 30;
            tomato.HorizontalAlignment = HorizontalAlignment.Stretch;

            Button btn_1 = new Button();
            btn_1.Content = "X";
            RelativePanel.SetAlignLeftWithPanel(btn_1, true);
            RelativePanel.SetAlignVerticalCenterWithPanel(btn_1, true);

            Button btn_2 = new Button();
            btn_2.Content = "O";
            RelativePanel.SetAlignRightWithPanel(btn_2, true);
            RelativePanel.SetAlignVerticalCenterWithPanel(btn_2, true);

            TextBlock txt = new TextBlock();
            txt.Text = "Timer";
            RelativePanel.SetAlignHorizontalCenterWithPanel(txt, true);
            RelativePanel.SetAlignVerticalCenterWithPanel(txt, true);

            tomato.Children.Add(btn_1);
            tomato.Children.Add(btn_2);
            tomato.Children.Add(txt);

            ListView_Timers.Items.Insert(1, tomato);
        }

        private void Button_TodoList_Click(object sender, RoutedEventArgs e)
        {
            SplitView_TodoList.IsPaneOpen = !SplitView_TodoList.IsPaneOpen;
        }

        private void Button_TimerList_Click(object sender, RoutedEventArgs e)
        {
            SplitView_TimerList.IsPaneOpen = !SplitView_TimerList.IsPaneOpen;
        }

        private async void Button_TimerStart_Click(object sender, RoutedEventArgs e)
        {
            timer_seconds = int.Parse(TextBox_TimerMinutes.Text) * 60 +
                            int.Parse(TextBox_TimerSeconds.Text);

            if (timer != null)
            {
                timer.Cancel();
            }
            
            timer = ThreadPoolTimer.CreatePeriodicTimer((source) =>
            {
                timer_seconds -= 1;
                
                Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High,
                    () =>
                    {
                        TextBox_TimerMinutes.Text = (timer_seconds / 60).ToString("D2");
                        TextBox_TimerSeconds.Text = (timer_seconds % 60).ToString("D2");
                    });

                if (timer_seconds == 0)
                {
                    source.Cancel();
                }
            }, tick_period);

            RelativePanel new_timer_panel = new RelativePanel();
            new_timer_panel.Width = SplitView_TimerList.OpenPaneLength - 20;

            Button btn_cancel = new Button();
            btn_cancel.Content = "Cancel";
            RelativePanel.SetAlignVerticalCenterWithPanel(btn_cancel, true);
            RelativePanel.SetAlignRightWithPanel(btn_cancel, true);

            TextBlock txt_progress = new TextBlock();
            txt_progress.Text = "0%";
            RelativePanel.SetAlignVerticalCenterWithPanel(txt_progress, true);
            RelativePanel.SetAlignLeftWithPanel(txt_progress, true);

            TextBlock txt_timer_center = new TextBlock();
            txt_timer_center.Text = ":";
            RelativePanel.SetAlignHorizontalCenterWithPanel(txt_timer_center, true);
            RelativePanel.SetAlignVerticalCenterWithPanel(txt_timer_center, true);

            TextBlock txt_minutes = new TextBlock();
            txt_minutes.Text = (timer_seconds / 60).ToString("D2");
            RelativePanel.SetLeftOf(txt_minutes, txt_timer_center);
            RelativePanel.SetAlignVerticalCenterWithPanel(txt_minutes, true);

            TextBlock txt_seconds = new TextBlock();
            txt_seconds.Text = (timer_seconds % 60).ToString("D2");
            RelativePanel.SetRightOf(txt_seconds, txt_timer_center);
            RelativePanel.SetAlignVerticalCenterWithPanel(txt_seconds, true);

            new_timer_panel.Children.Add(btn_cancel);
            new_timer_panel.Children.Add(txt_progress);
            new_timer_panel.Children.Add(txt_timer_center);
            new_timer_panel.Children.Add(txt_minutes);
            new_timer_panel.Children.Add(txt_seconds);

            XCoreTimer core_timer = new XCoreTimer(
                timer_seconds, txt_minutes, txt_seconds, Dispatcher);
            core_timer.Run();

            btn_cancel.Click += core_timer.Button_Cancel;

            ListView_Timers.Items.Add(new_timer_panel);
        }

        private void Button_TimerCancel_Click(object sender, RoutedEventArgs e)
        {
            timer.Cancel();
        }
    }
}
