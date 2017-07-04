using SitStandTimer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Windows.UI.Xaml;

namespace SitStandTimer
{
    public enum Mode
    {
        Sit,
        Stand
    }

    public enum TimerState
    {
        Running,
        Paused
    }

    public class TimeManager
    {
        private static TimeManager _instance;
        public static TimeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new TimeManager();
                }

                return _instance;
            }
        }

        private TimeManager()
        {
            CurrentMode = _modes[0];
            State = TimerState.Running;
            _currentModeStart = DateTime.Now;
            _timeRemainingInCurrentMode = _modeIntervals[CurrentMode];

            _toastNotifier = ToastNotificationManager.CreateToastNotifier();
        }

        public TimerState State { get; private set; }
        public Mode CurrentMode { get; private set; }
        public Mode NextMode
        {
            get
            {
                return getNextMode(CurrentMode);
            }
        }

        public void Initialize(SaveStateModel savedState)
        {
            DateTime now = DateTime.Now;

            State = savedState.TimerState;
            CurrentMode = savedState.CurrentMode;
            _timeRemainingInCurrentMode = savedState.TimeRemainingInCurrentMode;

            if (State == TimerState.Running)
            {
                // Use the saved state as a starting point and then figure out what mode we should currently be in based on the 
                // elapsed time.
                Mode mode = savedState.CurrentMode;
                
                DateTime modeStartTime = DateTime.FromBinary(savedState.CurrentModeStartTime);
                DateTime modeEndTime = modeStartTime + _modeIntervals[mode];

                while (modeEndTime < now)
                {
                    modeStartTime = modeEndTime;
                    mode = getNextMode(mode);
                    modeEndTime = modeStartTime + _modeIntervals[mode];
                }

                _currentModeStart = modeStartTime;
                CurrentMode = mode;
            }

            // This will finish initializing the _currentModeStart and the _timeRemainingInCurrentMode variables.
            GetTimeRemainingInCurrentMode();
        }

        public TimeSpan GetTimeRemainingInCurrentMode()
        {
            DateTime now = DateTime.Now;
            if (State == TimerState.Running)
            {
                TimeSpan timeInCurrentMode = now - _currentModeStart;
                TimeSpan maxTimeInCurrentMode = _modeIntervals[CurrentMode];

                if (timeInCurrentMode >= maxTimeInCurrentMode)
                {
                    // Time to switch modes
                    SkipToNextMode();
                    maxTimeInCurrentMode = _modeIntervals[CurrentMode];

                    timeInCurrentMode = TimeSpan.FromSeconds(0);
                }

                // Save the time remaining in case the user pauses the timer
                _timeRemainingInCurrentMode = maxTimeInCurrentMode - timeInCurrentMode;
            }
            else // Paused
            {
                // Update the current mode start time so when the user does resume the timer, we'll have accurate data
                _currentModeStart = now - (_modeIntervals[CurrentMode] - _timeRemainingInCurrentMode);
            }

            return _timeRemainingInCurrentMode;
        }

        public void SkipToNextMode()
        {
            CurrentMode = NextMode;
            _currentModeStart = DateTime.Now;
            _timeRemainingInCurrentMode = _modeIntervals[CurrentMode];

            // Update the notification queue
            ScheduleNotifications();
        }

        public void SetTimerState(TimerState newState)
        {
            if (newState != State)
            {
                State = newState;

                // Update the notification queue
                ScheduleNotifications();
            }
        }

        /// <summary>
        /// Clears the queue of currently scheduled notifications and schedules all notifications that should happen in the next 30 min.
        /// 30 min is chosen because our background task is guarenteed to run every 30 min (smallest possible interval).
        /// Each time the background task runs, it will call this function and set all notifications that should appear before the next time the task is able to run.
        /// </summary>
        public void ScheduleNotifications()
        {
            List<string> removedNotifications = new List<string>();
            List<string> addedNotifications = new List<string>();

            // First, clear the scheduled notification queue
            IReadOnlyList<ScheduledToastNotification> scheduledNotifications = _toastNotifier.GetScheduledToastNotifications();
            foreach (ScheduledToastNotification notification in scheduledNotifications)
            {
                removedNotifications.Add(notification.DeliveryTime.ToString());
                _toastNotifier.RemoveFromSchedule(notification);
            }

            // Clear any of our existing notifications from the user's action center
            // TODO: leaving this in for now to test. Want to make sure that past notifications fire properly
            //ToastNotificationManager.History.Clear();

            if (State == TimerState.Running)
            {
                // Schedule all notifications that will appear in the next 30 min
                DateTimeOffset now = DateTimeOffset.Now;
                TimeSpan thirtyMin = TimeSpan.FromMinutes(30);
                Mode modeToNotifyEnd = CurrentMode;
                DateTimeOffset nextNotificationTime = now + GetTimeRemainingInCurrentMode();

                while (nextNotificationTime - now < thirtyMin)
                {
                    Mode nextMode = getNextMode(modeToNotifyEnd);

                    XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText01);
                    IXmlNode textNode = toastXml.GetElementsByTagName("text")[0];
                    textNode.AppendChild(toastXml.CreateTextNode($"Time to {nextMode}! ({nextNotificationTime.ToString("t")})"));

                    ScheduledToastNotification notification = new ScheduledToastNotification(toastXml, nextNotificationTime);
                    _toastNotifier.AddToSchedule(notification);
                    addedNotifications.Add(notification.DeliveryTime.ToString());

                    modeToNotifyEnd = nextMode;

                    TimeSpan timeForMode = _modeIntervals[modeToNotifyEnd];
                    nextNotificationTime = nextNotificationTime + timeForMode;
                }
            }

            _lastDebugInfo = new DebugInfo()
            {
                LastRunTime = DateTime.Now.ToString(),
                NotificationsScheduled = addedNotifications.ToArray(),
                ScheduledNotificationsRemoved = removedNotifications.ToArray()
            };
        }

        public SaveStateModel GetCurrentModeInfo()
        {
            // Make sure the mode is up to date
            GetTimeRemainingInCurrentMode();
            
            return new SaveStateModel()
            {
                CurrentMode = CurrentMode,
                CurrentModeStartTime = _currentModeStart.ToBinary(),
                TimerState = State,
                TimeRemainingInCurrentMode = _timeRemainingInCurrentMode,
                LastRunDebugInfo = _lastDebugInfo
            };
        }

        private Mode getNextMode(Mode currentMode)
        {
            int currentModeIndex = _modes.IndexOf(currentMode);
            return _modes[(currentModeIndex + 1) % _modes.Count];
        }

        private DateTime _currentModeStart;
        private TimeSpan _timeRemainingInCurrentMode;
        private ToastNotifier _toastNotifier;
        private DebugInfo _lastDebugInfo;

        private static readonly Dictionary<Mode, TimeSpan> _modeIntervals = new Dictionary<Mode, TimeSpan>()
        {
            { Mode.Sit, TimeSpan.FromMinutes(15) },
            { Mode.Stand, TimeSpan.FromMinutes(15) }
        };
        private static readonly List<Mode> _modes = _modeIntervals.Keys.ToList();
    }
}
