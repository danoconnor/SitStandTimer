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
        Paused, // Temporary: if on a schedule we will resume running when the schedule starts again. If not on a schedule, then this is effectively the same as Stopped.
        Stopped // Long term: if on a schedule we will not run again until the user manually hits play
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
            NumLoopsRemaining = 1;
            _currentModeStart = DateTime.Now;
            TimeRemainingInCurrentMode = TimeSpan.MaxValue;

            _toastNotifier = ToastNotificationManager.CreateToastNotifier();
        }

        /// <summary>
        /// Note: The timer should switch to the Stopped state when NumLoopsRemaining == 0.
        /// </summary>
        public int NumLoopsRemaining { get; private set; }
        public ScheduleModel Schedule { get; private set; }
        public TimerState State { get; private set; }
        public ModeModel CurrentMode { get; private set; }
        public List<ModeModel> Modes { get; private set; }
        public TimeSpan TimeRemainingInCurrentMode { get; private set; }

        public ModeModel NextMode
        {
            get
            {
                (ModeModel nextMode, _) = getNextMode(CurrentMode);
                return nextMode;
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
            Modes = savedState?.Modes ?? new List<ModeModel>();

            if (!HasMultipleModes)
            {
                return;
            }

            DateTime now = DateTime.Now;

            Schedule = savedState.Schedule ?? new ScheduleModel() { ScheduleType = ScheduleType.Indefinite };
            NumLoopsRemaining = savedState.NumLoopsRemaining;
            State = savedState.TimerState;
            CurrentMode = savedState.CurrentMode;
            TimeRemainingInCurrentMode = savedState.TimeRemainingInCurrentMode;

            if (State == TimerState.Running)
            {
                // Use the saved state as a starting point and then figure out what mode we should currently be in based on the 
                // elapsed time.
                ModeModel mode = savedState.CurrentMode;
                
                DateTime modeStartTime = savedState.CurrentModeStartTime;
                DateTime modeEndTime = modeStartTime + mode.TimeInMode;

                bool modeChangedSinceLastRun = false;
                bool modesDidLoop = false;
                while (modeEndTime < now)
                {
                    modeStartTime = modeEndTime;
                    (mode, modesDidLoop) = getNextMode(mode);
                    modeEndTime = modeStartTime + mode.TimeInMode;
                    modeChangedSinceLastRun = true;

                    if (modesDidLoop && Schedule.ScheduleType == ScheduleType.NumTimes)
                    {
                        NumLoopsRemaining--;
                    }
                }

                _currentModeStart = modeStartTime;
                CurrentMode = mode;

                if (getStateBasedOnScheduleAndCurrentState() == TimerState.Running &&
                    modeChangedSinceLastRun && 
                    now - savedState.LastRunDebugInfo.LastRunTime > TimeSpan.FromMinutes(30))
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

            // TODO: TEST CODE, REMOVE LATER
            Schedule.ScheduleType = ScheduleType.NumTimes;
            NumLoopsRemaining = 1;

            // Finish up initialization to make sure the state is correct based on the schedule and finish initializing the 
            // _currentModeStart and the _timeRemainingInCurrentMode variables
            Update();
        }

        public void UpdateModes(List<ModeModel> modes)
        {
            bool wasPreviouslyInitialized = HasMultipleModes;

            Modes = modes;
            ModeModel newCurrentMode = Modes.FirstOrDefault(mode => (mode.Id == CurrentMode?.Id));

            if ((!wasPreviouslyInitialized || newCurrentMode == null) && HasMultipleModes)
            {
                // Reset the mode if this we went from an uninitialized to an initialized state or if the current mode has been deleted
                CurrentMode = Modes[0];
                _currentModeStart = DateTime.Now;
                TimeRemainingInCurrentMode = CurrentMode.TimeInMode;
                Update();
            }
            else if (newCurrentMode != null)
            {
                // Update the current mode to get any edits that might have been made to it.
                CurrentMode = newCurrentMode;
            }

            // Update the notification queue since things have been changed
            ScheduleNotifications();
        }

        /// <summary>
        /// Called from the MainPageVM timer every second, so that the Mode, State, and TimeRemainingInCurrentMode values are all updated.
        /// </summary>
        public void Update()
        {
            if (!HasMultipleModes)
            {
                // If we have less than two modes, then there is no need to do anything
                return;
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
                TimeRemainingInCurrentMode = maxTimeInCurrentMode - timeInCurrentMode;
            }
            else // Paused or Stopped
            {
                // Update the current mode start time so when the user does resume the timer, we'll have accurate data
                _currentModeStart = now - (CurrentMode.TimeInMode - TimeRemainingInCurrentMode);
            }

            // Check the Schedule and see if we need to do any updating based on that
            TimerState newState = getStateBasedOnScheduleAndCurrentState();
            if (State != newState && newState == TimerState.Stopped)
            {
                // Revert back to the first mode in the list
                CurrentMode = Modes[0];
                TimeRemainingInCurrentMode = CurrentMode.TimeInMode;
                _currentModeStart = DateTime.Now;
                NumLoopsRemaining = Schedule.NumTimesToLoop ?? 1;
            }

            State = newState;
        }

        public void SkipToNextMode()
        {
            (ModeModel nextMode, bool didModesLoop) = getNextMode(CurrentMode);

            if (didModesLoop)
            {
                // Just continue even if this goes negative. The next Update() call will handle setting the state.
                NumLoopsRemaining--;
            }

            CurrentMode = nextMode;
            _currentModeStart = DateTime.Now;
            TimeRemainingInCurrentMode = CurrentMode.TimeInMode;

            // Update the notification queue
            ScheduleNotifications();
        }

        public void SetTimerState(TimerState newState)
        {
            if (newState != State)
            {
                State = newState;

                if (State == TimerState.Stopped)
                {
                    NumLoopsRemaining = Schedule.NumTimesToLoop ?? 1;
                }

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

            // Make sure everything is up to date
            Update();

            if (State == TimerState.Running)
            {
                // Schedule all notifications that will appear in the next 30 min
                DateTimeOffset now = DateTimeOffset.Now;
                TimeSpan thirtyMin = TimeSpan.FromMinutes(30);
                ModeModel modeToNotifyEnd = CurrentMode;
                DateTimeOffset nextNotificationTime = now + TimeRemainingInCurrentMode;

                while (nextNotificationTime - now < thirtyMin)
                {
                    (ModeModel nextMode, bool modesDidLoop) = getNextMode(modeToNotifyEnd);

                    if (modesDidLoop && Schedule.ScheduleType == ScheduleType.NumTimes && NumLoopsRemaining-- ==  0)
                    {
                        // We have finished our last loop, do not schedule any more notifications
                        break;
                    }

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
            Update();
            
            return new SaveStateModel()
            {
                Modes = Modes,
                CurrentMode = CurrentMode,
                CurrentModeStartTime = _currentModeStart,
                TimerState = State,
                TimeRemainingInCurrentMode = TimeRemainingInCurrentMode,
                Schedule = Schedule,
                NumLoopsRemaining = NumLoopsRemaining,
                LastRunDebugInfo = _lastDebugInfo
            };
        }

        /// <summary>
        /// Used by the MainPageVM to display different text depending on whether the timer will automatically stop after the current mode.
        /// </summary>
        /// <returns></returns>
        public bool WillStopAfterCurrentMode()
        {
            if (!HasMultipleModes)
            {
                return false;
            }

            bool willStop = false;
            if (Schedule.ScheduleType == ScheduleType.Scheduled)
            {
                DateTime endOfCurrentMode = DateTime.Now + TimeRemainingInCurrentMode;
                TimeSpan endOfModeTime = new TimeSpan(endOfCurrentMode.Hour, endOfCurrentMode.Minute, endOfCurrentMode.Second);

                willStop = (Schedule.StartTime < endOfModeTime || Schedule.EndTime < endOfModeTime);
            }
            else if (Schedule.ScheduleType == ScheduleType.NumTimes)
            {
                willStop = (NumLoopsRemaining == 1 && CurrentMode.Id == Modes[Modes.Count - 1].Id);
            }

            return willStop;
        }

        /// <summary>
        /// Returns a tuple where the first item is the next mode and the second item is a bool indicating if the modes have looped.
        /// modesDidLoop is useful when Schedule.ScheduleType is set to NumLoops, so we can check if we have looped too many times and update accordingly.
        /// </summary>
        private (ModeModel nextMode, bool modesDidLoop) getNextMode(ModeModel currentMode)
        {
            if (!HasMultipleModes)
            {
                return (null, false);
            }

            int currentModeIndex = Modes.FindIndex(mode => mode.Id == currentMode.Id);
            Debug.Assert(currentModeIndex >= 0 || !HasMultipleModes);

            int nextIndex = (currentModeIndex + 1);
            bool willLoop = (nextIndex >= Modes.Count);
            nextIndex = willLoop ? 0 : nextIndex;

            return (Modes[nextIndex], willLoop);
        }

        private TimerState getStateBasedOnScheduleAndCurrentState()
        {
            TimerState newState;
            if (Schedule.ScheduleType == ScheduleType.Indefinite || NumLoopsRemaining > 0)
            {
                // Keep whatever the current state is.
                // If we are in Scheduled mode, but NumLoopsRemaining > 0 
                // then the user has manually kicked off a run outside of scheduled hours so just keep doing that.
                newState = State;
            }
            else if (Schedule.ScheduleType == ScheduleType.NumTimes || State == TimerState.Stopped)
            {
                // There are no loops remaining or the user has manually stopped the timer.
                newState = TimerState.Stopped;
            }
            else
            {
                // We are in scheduled mode and the state is either Running or Paused.
                // Figure out if we are inside scheduled hours.

                DateTime now = DateTime.Now;

                if (!Schedule.Days.Contains(now.DayOfWeek))
                {
                    // Outside of scheduled days of week
                    newState = TimerState.Paused;
                }
                else
                {
                    TimeSpan nowSpan = new TimeSpan(now.Hour, now.Minute, 0);
                    if (nowSpan >= Schedule.StartTime && nowSpan < Schedule.EndTime)
                    {
                        newState = TimerState.Running;
                    }
                    else
                    {
                        newState = TimerState.Paused;
                    }
                }
            }
            
            return newState;
        }

        private DateTime _currentModeStart;
        private ToastNotifier _toastNotifier;
        private DebugInfo _lastDebugInfo;
    }
}
