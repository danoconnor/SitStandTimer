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
            _currentModeStart = DateTime.Now;

            _toastNotifier = ToastNotificationManager.CreateToastNotifier();
        }

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
            // Use the saved state as a starting point and then figure out what mode we should currently be in.
            // TODO: figure out current mode based on elapsed time since the save state
            CurrentMode = savedState.CurrentMode;
            _currentModeStart = new DateTime(savedState.CurrentModeStartTime);
        }

        public TimeSpan GetTimeRemainingInCurrentMode()
        {
            DateTime now = DateTime.Now;
            TimeSpan timeInCurrentMode = now - _currentModeStart;
            TimeSpan maxTimeInCurrentMode = _modeIntervals[CurrentMode];

            if (timeInCurrentMode >= maxTimeInCurrentMode)
            {
                // Time to switch modes
                CurrentMode = NextMode;

                _currentModeStart = now;
                timeInCurrentMode = TimeSpan.FromSeconds(0);
            }

            return maxTimeInCurrentMode - timeInCurrentMode;
        }

        /// <summary>
        /// Clears the queue of currently scheduled notifications and schedules all notifications that should happen in the next 30 min.
        /// 30 min is chosen because our background task is guarenteed to run every 30 min (smallest possible interval).
        /// Each time the background task runs, it will call this function and set all notifications that should appear before the next time the task is able to run.
        /// </summary>
        public void ScheduleNotifications()
        {
            // First, clear the scheduled notification queue
            IReadOnlyList<ScheduledToastNotification> scheduledNotifications = _toastNotifier.GetScheduledToastNotifications();
            foreach (ScheduledToastNotification notification in scheduledNotifications)
            {
                _toastNotifier.RemoveFromSchedule(notification);
            }

            // Clear any of our existing notifications from the user's action center
            ToastNotificationManager.History.Clear();

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

                modeToNotifyEnd = nextMode;

                TimeSpan timeForMode = _modeIntervals[modeToNotifyEnd];
                nextNotificationTime = nextNotificationTime + timeForMode;
            }
        }

        public SaveStateModel GetCurrentModeInfo()
        {
            // Make sure the mode is up to date
            GetTimeRemainingInCurrentMode();

            return new SaveStateModel()
            {
                CurrentMode = CurrentMode,
                CurrentModeStartTime = _currentModeStart.ToBinary()
            };
        }

        private Mode getNextMode(Mode currentMode)
        {
            int currentModeIndex = _modes.IndexOf(currentMode);
            return _modes[(currentModeIndex + 1) % _modes.Count];
        }

        private DateTime _currentModeStart;
        private ToastNotifier _toastNotifier;

        private static readonly Dictionary<Mode, TimeSpan> _modeIntervals = new Dictionary<Mode, TimeSpan>()
        {
            { Mode.Sit, TimeSpan.FromMinutes(.5) },
            { Mode.Stand, TimeSpan.FromMinutes(.5) }
        };
        private static readonly List<Mode> _modes = _modeIntervals.Keys.ToList();
    }
}
