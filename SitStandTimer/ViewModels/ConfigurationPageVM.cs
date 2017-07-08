using SitStandTimer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SitStandTimer.ViewModels
{
    public class ConfigurationPageVM : ViewModelBase
    {
        public ConfigurationPageVM()
        {
            Modes = new ObservableCollection<ModeModel>();
            
            foreach (ModeModel mode in TimeManager.Instance.Modes)
            {
                Modes.Add(mode);
            }
        }

        public ObservableCollection<ModeModel> Modes { get; set; }

        public string NewModeName { get; set; }
        public string NewModeHours { get; set; }
        public string NewModeMinutes { get; set; }
        public string NewModeSeconds { get; set; }

        public void ItemsReordered()
        {
            TimeManager.Instance.UpdateModes(Modes);
        }

        public void SaveNewMode()
        {
            double newModeHours, newModeMinutes, newModeSeconds;

            double.TryParse(NewModeHours, out newModeHours);
            double.TryParse(NewModeMinutes, out newModeMinutes);
            double.TryParse(NewModeSeconds, out newModeSeconds);

            double totalHours = newModeHours + (newModeMinutes / 60) + (newModeSeconds / (60 * 60));
            TimeSpan time = TimeSpan.FromHours(totalHours);
            
            // Only save the mode if the user has entered something
            if (time.TotalSeconds >= 1 && !string.IsNullOrWhiteSpace(NewModeName))
            {
                ModeModel newMode = new ModeModel()
                {
                    ModeName = NewModeName,
                    TimeInMode = TimeSpan.FromHours(totalHours)
                };

                Modes.Add(newMode);
                TimeManager.Instance.UpdateModes(Modes);
            }            
            
            ClearNewModeStrings();
        }

        public void ClearNewModeStrings()
        {
            NewModeHours = "";
            NewModeMinutes = "";
            NewModeSeconds = "";
            NewModeName = "";

            RaisePropertyChanged(nameof(NewModeHours));
            RaisePropertyChanged(nameof(NewModeMinutes));
            RaisePropertyChanged(nameof(NewModeSeconds));
            RaisePropertyChanged(nameof(NewModeName));
        }

        public void DeleteMode(ModeModel mode)
        {
            Modes.Remove(mode);
            TimeManager.Instance.UpdateModes(Modes);
        }
    }
}
