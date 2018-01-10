using SitStandTimer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Windows.UI.Xaml;

namespace SitStandTimer
{ 
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
            Modes = new List<ModeModel>();

            CurrentMode = null;
            State = TimerState.Running;
            _currentModeStart = DateTime.Now;
            _timeRemainingInCurrentMode = TimeSpan.MaxValue;

            _toastNotifier = ToastNotificationManager.CreateToastNotifier();
        }

        public TimerState State { get; private set; }
        public ModeModel CurrentMode { get; private set; }
        public List<ModeModel> Modes { get; private set; }
        public ModeModel NextMode
        {
            get
            {
                return getNextMode(CurrentMode);
            }
        }
        public bool HasMultipleModes
        {
            get
            {
                return Modes?.Count > 1;
            }
        }

        public void Initialize(SaveStateModel savedState)
        {
            Modes = savedState?.Modes;

            if (!HasMultipleModes)
            {
                return;
            }

            DateTime now = DateTime.Now;

            State = savedState.TimerState;
            CurrentMode = savedState.CurrentMode;
            _timeRemainingInCurrentMode = savedState.TimeRemainingInCurrentMode;

            if (State == TimerState.Running)
            {
                // Use the saved state as a starting point and then figure out what mode we should currently be in based on the 
                // elapsed time.
                ModeModel mode = savedState.CurrentMode;
                
                DateTime modeStartTime = savedState.CurrentModeStartTime;
                DateTime modeEndTime = modeStartTime + mode.TimeInMode;

                bool modeChangedSinceLastRun = false;
                while (modeEndTime < now)
                {
                    modeStartTime = modeEndTime;
                    mode = getNextMode(mode);
                    modeEndTime = modeStartTime + mode.TimeInMode;
                    modeChangedSinceLastRun = true;
                }

                _currentModeStart = modeStartTime;
                CurrentMode = mode;

                if (modeChangedSinceLastRun && now - savedState.LastRunDebugInfo.LastRunTime > TimeSpan.FromMinutes(30))
                {
                    // If we have not run in the past 30 min, then the user's computer was off.
                    // In this case figure out if the mode has changed at all since the last notification and if so,
                    // we will force a notification immediately to notify the user which mode they are in.

                    XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText01);
                    IXmlNode textNode = toastXml.GetElementsByTagName("text")[0];
                    textNode.AppendChild(toastXml.CreateTextNode($"Time to {CurrentMode}! ({modeStartTime.ToString("t")})"));

                    ToastNotification notification = new ToastNotification(toastXml);
                    _toastNotifier.Show(notification);
                }
            }

            // This will finish initializing the _currentModeStart and the _timeRemainingInCurrentMode variables.
            GetTimeRemainingInCurrentMode();
        }

        public void UpdateModes(ObservableCollection<ModeModel> modes)
        {
            bool wasPreviouslyInitialized = HasMultipleModes;

            Modes = modes.ToList();
            bool currentModeExists = Modes.Any(mode => (mode.Id == CurrentMode?.Id));

            if ((!wasPreviouslyInitialized || !currentModeExists) && HasMultipleModes)
            {
                // Reset the mode if this we went from an uninitialized to an initialized state or if the current mode has been deleted
                CurrentMode = Modes[0];
                _currentModeStart = DateTime.Now;
                _timeRemainingInCurrentMode = CurrentMode.TimeInMode;
                GetTimeRemainingInCurrentMode();
            }

            // Update the notification queue since things have been changed
            ScheduleNotifications();
        }

        public TimeSpan GetTimeRemainingInCurrentMode()
        {
            if (!HasMultipleModes)
            {
                // If we don't have more than one mode, just return a max value since there is nothing to calculate
                return TimeSpan.MaxValue;
            }

            DateTime now = DateTime.Now;
            if (State == TimerState.Running)
            {
                TimeSpan timeInCurrentMode = now - _currentModeStart;
                TimeSpan maxTimeInCurrentMode = CurrentMode.TimeInMode;

                if (timeInCurrentMode >= maxTimeInCurrentMode)
                {
                    // Time to switch modes
                    SkipToNextMode();
                    maxTimeInCurrentMode = CurrentMode.TimeInMode;

                    timeInCurrentMode = TimeSpan.FromSeconds(0);
                }

                // Save the time remaining in case the user pauses the timer
                _timeRemainingInCurrentMode = maxTimeInCurrentMode - timeInCurrentMode;
            }
            else // Paused
            {
                // Update the current mode start time so when the user does resume the timer, we'll have accurate data
                _currentModeStart = now - (CurrentMode.TimeInMode - _timeRemainingInCurrentMode);
            }

            return _timeRemainingInCurrentMode;
        }

        public void SkipToNextMode()
        {
            CurrentMode = NextMode;
            _currentModeStart = DateTime.Now;
            _timeRemainingInCurrentMode = CurrentMode.TimeInMode;

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
            if (!HasMultipleModes)
            {
                // Don't try to schedule notitfications if the user has not set any modes
                return;
            }

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
                ModeModel modeToNotifyEnd = CurrentMode;
                DateTimeOffset nextNotificationTime = now + GetTimeRemainingInCurrentMode();

                while (nextNotificationTime - now < thirtyMin)
                {
                    ModeModel nextMode = getNextMode(modeToNotifyEnd);

                    XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText01);
                    IXmlNode textNode = toastXml.GetElementsByTagName("text")[0];
                    textNode.AppendChild(toastXml.CreateTextNode($"Time to {nextMode}! ({nextNotificationTime.ToString("t")})"));

                    ScheduledToastNotification notification = new ScheduledToastNotification(toastXml, nextNotificationTime);
                    _toastNotifier.AddToSchedule(notification);
                    addedNotifications.Add(notification.DeliveryTime.ToString());

                    modeToNotifyEnd = nextMode;

                    TimeSpan timeForMode = modeToNotifyEnd.TimeInMode;
                    nextNotificationTime = nextNotificationTime + timeForMode;
                }
            }

            _lastDebugInfo = new DebugInfo()
            {
                LastRunTime = DateTime.Now,
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
                Modes = Modes,
                CurrentMode = CurrentMode,
                CurrentModeStartTime = _currentModeStart,
                TimerState = State,
                TimeRemainingInCurrentMode = _timeRemainingInCurrentMode,
                LastRunDebugInfo = _lastDebugInfo
            };
        }

        private ModeModel getNextMode(ModeModel currentMode)
        {
            int currentModeIndex = Modes.FindIndex(mode => mode.Id == currentMode.Id);
            Debug.Assert(currentModeIndex >= 0);

            return HasMultipleModes ? 
                Modes[(currentModeIndex + 1) % Modes.Count] :
                null;
        }

        private DateTime _currentModeStart;
        private TimeSpan _timeRemainingInCurrentMode;
        private ToastNotifier _toastNotifier;
        private DebugInfo _lastDebugInfo;
    }
}
