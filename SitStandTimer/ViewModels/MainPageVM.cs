using SitStandTimer.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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

            if (TimeManager.Instance.Schedule.ScheduleType == ScheduleType.NumTimes)
            {
                // Need to update the change state icon in case this mode was the last one and we automatically switched to a pause state
                RaisePropertyChanged(nameof(ChangeStateIcon));
            }
        }

        private void timerTick(object sender, object args)
        {
            Symbol currentStateIcon = ChangeStateIcon;

            // Get the updated remaining time from TimeManager
            TimeManager.Instance.Update();
            TimeSpan remainingTime = TimeManager.Instance.TimeRemainingInCurrentMode;

            // Update icon if the timer automatically stopped or started
            if (currentStateIcon != ChangeStateIcon)
            {
                RaisePropertyChanged(nameof(ChangeStateIcon));
            }

            string timeFormat = @"hh\:mm\:ss";
            if (remainingTime < TimeSpan.FromHours(1))
            {
                // Don't show the hours if the remaining time is less than an hour
                timeFormat = @"mm\:ss";
            }

            ModeText = TimeManager.Instance.CurrentMode.ModeName.ToUpper();

            if (TimeManager.Instance.WillStopAfterCurrentMode())
            {
                ScheduleModel schedule = TimeManager.Instance.Schedule;
                if (schedule.ScheduleType == ScheduleType.Scheduled)
                {
                    DayOfWeek today = DateTime.Now.DayOfWeek;
                    DayOfWeek nextScheduledDay = today + 1;
                    while (!schedule.Days.Contains(nextScheduledDay))
                    {
                        nextScheduledDay++;
                    }

                    string nextDayStr = nextScheduledDay.ToString();
                    if (nextScheduledDay - today == 1)
                    {
                        nextDayStr = "tomorrow";
                    }

                    TimeLeftText = $"The timer will automatically pause at {timeSpanToString(schedule.EndTime)}. It will automatically resume at {timeSpanToString(schedule.StartTime)} {nextDayStr}.";
                }
                else if (schedule.ScheduleType == ScheduleType.NumTimes)
                {
                    TimeLeftText = $"The timer will automatically stop in {remainingTime.ToString(timeFormat)} because will have completed all {schedule.NumTimesToLoop} loops.";
                }
                else
                {
                    Debug.Assert(false, "We should not be here with an infinite schedule");
                }
            }
            else
            {
                TimeLeftText = $"{remainingTime.ToString(timeFormat)} until you switch to {TimeManager.Instance.NextMode?.ModeName.ToLower()}";
            }

            RaisePropertyChanged(nameof(ModeText));
            RaisePropertyChanged(nameof(TimeLeftText));
        }

        private string timeSpanToString(TimeSpan time)
        {
            string timeOfDay = "am";
            int hours = time.Hours;

            if (hours > 12)
            {
                hours -= 12;
                timeOfDay = "pm";
            }

            int minutes = time.Minutes;
            string minutesStr = minutes.ToString();
            if (minutes < 10)
            {
                minutesStr.Insert(0, "0");
            }

            return $"{hours}:{minutesStr} {timeOfDay}";
        }

        private DispatcherTimer _timer;
    }
}
