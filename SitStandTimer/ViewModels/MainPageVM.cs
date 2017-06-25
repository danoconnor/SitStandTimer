using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace SitStandTimer.ViewModels
{
    public class MainPageVM : ViewModelBase
    {
        public MainPageVM()
        {
            _timer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += timerTick;
            _timer.Start();

            // Initialize the strings without waiting 1 second for the timer tick to occur.
            timerTick(null, null);
        }

        public string ModeText { get; private set; }
        public string TimeLeftText { get; private set; }

        private void timerTick(object sender, object args)
        {
            // Get the updated remaining time from TimeManager
            TimeSpan remainingTime = TimeManager.Instance.GetTimeRemainingSeconds();

            ModeText = TimeManager.Instance.CurrentMode.ToString().ToUpper();
            TimeLeftText = $"{remainingTime.ToString(@"mm\:ss")} until you can {TimeManager.Instance.NextMode.ToString().ToUpper()}";

            RaisePropertyChanged(nameof(ModeText));
            RaisePropertyChanged(nameof(TimeLeftText));
        }

        private DispatcherTimer _timer;
    }
}
