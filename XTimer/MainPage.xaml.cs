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
using System.Diagnostics;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace XTimer
{
    // A convenient timer class
    public class XCoreTimer
    {
        public int Total_seconds { get; set; }
        public int Seconds_left { get; set; }

        private double _update_seconds;
        private ThreadPoolTimer core_timer;

        // UI-related
        private List<object> txt_minutes_to_update;
        private List<object> txt_seconds_to_update;
        public Windows.UI.Core.CoreDispatcher Core_dispatcher { get; set; }

        public XCoreTimer(
            int total_seconds, Windows.UI.Core.CoreDispatcher core_dispatcher,
            double update_period_in_seconds=1.0)
        {
            Seconds_left = Total_seconds = total_seconds;
            _update_seconds = update_period_in_seconds;

            Core_dispatcher = core_dispatcher;
            txt_minutes_to_update = new List<object>();
            txt_seconds_to_update = new List<object>();
        }

        public async void Run()
        {
            core_timer = ThreadPoolTimer.CreatePeriodicTimer(
                (source) => 
                {
                    Seconds_left -= 1;

                    Core_dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High,
                        () =>
                        {
                            int seconds = Seconds_left % 60;

                            // update second textblocks
                            foreach (object txt_seconds in txt_seconds_to_update)
                            {
                                if (txt_seconds.GetType() == typeof(TextBlock))
                                {
                                    TextBlock txt = (TextBlock)txt_seconds;
                                    txt.Text = seconds.ToString("D2");
                                }
                                else if (txt_seconds.GetType() == typeof(TextBox))
                                {
                                    TextBox txt = (TextBox)txt_seconds;
                                    txt.Text = seconds.ToString("D2");
                                }
                                else
                                {
                                    Debug.WriteLine("Unknown type in txt_seconds_to_update");
                                }
                            }

                            // update minute textblocks
                            if (seconds == 59)
                            {
                                int minutes = Seconds_left / 60;
                                foreach (object txt_minutes in txt_minutes_to_update)
                                {
                                    if (txt_minutes.GetType() == typeof(TextBlock))
                                    {
                                        TextBlock txt = (TextBlock)txt_minutes;
                                        txt.Text = minutes.ToString("D2");
                                    }
                                    else if (txt_minutes.GetType() == typeof(TextBox))
                                    {
                                        TextBox txt = (TextBox)txt_minutes;
                                        txt.Text = minutes.ToString("D2");
                                    }
                                    else
                                    {
                                        Debug.WriteLine("Unknown type in txt_minutes_to_update");
                                    }
                                }
                            }
                            
                        });

                    if (Seconds_left == 0)
                    {
                        source.Cancel();
                    }
                }, 
                TimeSpan.FromSeconds(_update_seconds));
        }

        public void BindTimer(
            object timer_minutes, object timer_seconds, 
            Button cancel_button=null)
        {
            if (!txt_minutes_to_update.Contains(timer_minutes))
            {
                txt_minutes_to_update.Add(timer_minutes);
            }

            if (!txt_seconds_to_update.Contains(timer_seconds))
            {
                txt_seconds_to_update.Add(timer_seconds);
            }

            if (cancel_button != null)
            {
                cancel_button.Click += Button_Cancel;
            }
        }

        public void UnbindTimer(
            object timer_minutes, object timer_seconds, 
            Button cancel_button=null)
        {
            txt_minutes_to_update.Remove(timer_minutes);
            txt_seconds_to_update.Remove(timer_seconds);
            if (cancel_button != null)
            {
                cancel_button.Click -= Button_Cancel;
            }
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
        public XCoreTimer CurrentTimer { get; set; }

        public MainPage()
        {
            this.InitializeComponent();

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
            int timer_seconds = int.Parse(TextBox_TimerMinutes.Text) * 60 +
                            int.Parse(TextBox_TimerSeconds.Text);

            // Create a new timer UI at the timer's list
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

            // Bind a new core timer object to the UI
            XCoreTimer core_timer = new XCoreTimer(timer_seconds, Dispatcher);
            core_timer.BindTimer(txt_minutes, txt_seconds, btn_cancel);
            if (CurrentTimer != null)
            {
                CurrentTimer.UnbindTimer(
                    TextBox_TimerMinutes, TextBox_TimerSeconds, Button_TimerCancel);
            }
            CurrentTimer = core_timer;
            core_timer.BindTimer(
                TextBox_TimerMinutes, TextBox_TimerSeconds, Button_TimerCancel);
            core_timer.Run();

            // Add the new timer to the list view
            ListView_Timers.Items.Add(new_timer_panel);

        }

        private void Button_TimerCancel_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void Button_NewTimer_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentTimer != null)
            {
                CurrentTimer.UnbindTimer(
                    TextBox_TimerMinutes, TextBox_TimerSeconds, Button_TimerCancel);
                CurrentTimer = null;
            }

        }
    }
}
