using SitStandTimer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SitStandTimer.ViewModels
{
    public class ModeVM: ViewModelBase
    {
        public ModeModel Mode { get; private set; }
        public string ModeString
        {
            get
            {
                return Mode?.ToString() ?? "";
            }
        }

        public bool IsEditMode { get; private set; }

        // When the user is changing mode values, we'll temporarily store the new values here in case the user cancels the edit or the value is invalid.
        public string EditModeName { get; set; }
        public string EditModeHours { get; set; }
        public string EditModeMinutes { get; set; }
        public string EditModeSeconds { get; set; }

        public ModeVM()
        {
            // If we aren't initialized with a mode, then this is a new mode that the user is creating
            IsEditMode = true;
        }

        public ModeVM(ModeModel mode)
        {
            Mode = mode;
            IsEditMode = false;
        }

        public object StartEditMode()
        {
            if (Mode != null)
            {
                EditModeName = Mode.ModeName;
                EditModeHours = Mode.TimeInMode.Hours.ToString();
                EditModeMinutes = Mode.TimeInMode.Minutes.ToString();
                EditModeSeconds = Mode.TimeInMode.Seconds.ToString();
            }
            else
            {
                EditModeName = "";
                EditModeHours = "";
                EditModeMinutes = "";
                EditModeSeconds = "";
            }

            IsEditMode = true;

            RaisePropertyChanged(nameof(EditModeName));
            RaisePropertyChanged(nameof(EditModeHours));
            RaisePropertyChanged(nameof(EditModeMinutes));
            RaisePropertyChanged(nameof(EditModeSeconds));
            RaisePropertyChanged(nameof(IsEditMode));

            return null;
        }

        public void CancelChanges()
        {
            IsEditMode = false;
            RaisePropertyChanged(nameof(IsEditMode));
        }

        /// <summary>
        /// Update the Mode property with the temporary changes, if the changes are valid.
        /// </summary>
        /// <returns>False if the changes were invalid. True if the save succeeded.</returns>
        public bool SaveChanges()
        {
            IsEditMode = false;
            RaisePropertyChanged(nameof(IsEditMode));

            // If the text box was empty, default to zero
            EditModeHours = EditModeHours != null && EditModeHours.Length > 0 ? EditModeHours : "0";
            EditModeMinutes = EditModeMinutes != null && EditModeMinutes.Length > 0 ? EditModeMinutes : "0";
            EditModeSeconds = EditModeSeconds != null && EditModeSeconds.Length > 0 ? EditModeSeconds : "0";

            if (EditModeName == null || 
                EditModeName.Length == 0 ||
                !int.TryParse(EditModeHours, out int hours) ||
                !int.TryParse(EditModeMinutes, out int minutes) ||
                !int.TryParse(EditModeSeconds, out int seconds) ||
                (hours == 0 && minutes == 0 && seconds == 0))
            {
                // Invalid mode, fail silently.
                return false;
            }

            Guid modeId = Mode?.Id ?? Guid.NewGuid();

            Mode = new ModeModel()
            {
                ModeName = EditModeName,
                TimeInMode = new TimeSpan(hours, minutes, seconds),
                Id = modeId
            };

            RaisePropertyChanged(nameof(ModeString));
            return true;
        }
    }
}
