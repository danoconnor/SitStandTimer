using System;
using System.Collections.Generic;
using System.Linq;
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
            CurrentMode = _modeIntervals.Keys.ToArray()[0];
            _currentModeStart = DateTime.Now;
        }

        public Mode CurrentMode { get; private set; }
        public Mode NextMode
        {
            get
            {
                int currentModeIndex = _modes.IndexOf(CurrentMode);
                return _modes[(currentModeIndex + 1) % _modes.Count];
            }
        }

        public TimeSpan GetTimeRemainingSeconds()
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

        private DateTime _currentModeStart;

        private static readonly Dictionary<Mode, TimeSpan> _modeIntervals = new Dictionary<Mode, TimeSpan>()
        {
            { Mode.Sit, TimeSpan.FromMinutes(.25) },
            { Mode.Stand, TimeSpan.FromMinutes(.25) }
        };
        private static readonly List<Mode> _modes = _modeIntervals.Keys.ToList();
    }
}
