using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SitStandTimer.ViewModels
{
    public class MainPageVM : ViewModelBase
    {
        public MainPageVM()
        {
            if (TimeManager.Instance.HasMultipleModes)
            {
                HasMultipleModes = true;
                _timer = new DispatcherTimer()
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                _timer.Tick += timerTick;
                _timer.Start();

                // Initialize the strings without waiting 1 second for the timer tick to occur.
                timerTick(null, null);
            }
            else
            {
                HasMultipleModes = false;
            }
        }

        public bool HasMultipleModes { get; private set; }
        public string ModeText { get; private set; }
        public string TimeLeftText { get; private set; }
        public Symbol ChangeStateIcon
        {
            get
            {
                if (TimeManager.Instance.State == TimerState.Running)
                {
                    return Symbol.Pause;
                }
                else // We are paused
                {
                    return Symbol.Play;
                }
            }
        }

        public Symbol NextIcon
        {
            get
            {
                return Symbol.Next;
            }
        }

        public void SwitchState()
        {
            if (TimeManager.Instance.State == TimerState.Running)
            {
                TimeManager.Instance.SetTimerState(TimerState.Paused);
            }
            else // We are currently paused
            {
                TimeManager.Instance.SetTimerState(TimerState.Running);
            }

            // Update the button icon
            RaisePropertyChanged(nameof(ChangeStateIcon));
        }

        public void SkipToNextMode()
        {
            TimeManager.Instance.SkipToNextMode();
            timerTick(null, null);
        }

        private void timerTick(object sender, object args)
        {
            // Get the updated remaining time from TimeManager
            TimeSpan remainingTime = TimeManager.Instance.GetTimeRemainingInCurrentMode();

            string timeFormat = @"hh\:mm\:ss";
            if (remainingTime < TimeSpan.FromHours(1))
            {
                // Don't show the hours if the remaining time is less than an hour
                timeFormat = @"mm\:ss";
            }

            ModeText = TimeManager.Instance.CurrentMode.ToUpper();
            TimeLeftText = $"{remainingTime.ToString(timeFormat)} until you switch to {TimeManager.Instance.NextMode.ToLower()}";

            RaisePropertyChanged(nameof(ModeText));
            RaisePropertyChanged(nameof(TimeLeftText));
        }

        private DispatcherTimer _timer;
    }
}
