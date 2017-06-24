using System;
using System.Collections.Generic;
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
            // Default to standing
            CurrentMode = Mode.Stand;
            _currentModeStart = DateTime.Now;

            _timer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += timerTick;
        }

        private void timerTick(object sender, object e)
        {
            
        }

        public Mode CurrentMode { get; private set; }

        private DateTime _currentModeStart;
        private DispatcherTimer _timer;

        private static readonly Dictionary<Mode, TimeSpan> _modeIntervals = new Dictionary<Mode, TimeSpan>()
        {
            { Mode.Sit, TimeSpan.FromMinutes(60) },
            { Mode.Stand, TimeSpan.FromMinutes(60) }
        };
    }
}
