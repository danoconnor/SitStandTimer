using SitStandTimer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SitStandTimer.ViewModels
{
    public class ConfigurationPageVM : ViewModelBase
    {
        public ConfigurationPageVM()
        {
            Modes = new ObservableCollection<ModeVM>();
            ShowAddModeButton = true;
            _selectedScheduleType = TimeManager.Instance.Schedule.ScheduleType;

            NumTimesOptions = Enumerable.Range(1, 100);
            
            foreach (ModeModel mode in TimeManager.Instance.Modes)
            {
                Modes.Add(new ModeVM(mode));
            }
        }

        public ObservableCollection<ModeVM> Modes { get; set; }

        public bool ShowAddModeButton { get; private set; }

        public List<ScheduleType> ScheduleTypes
        {
            get
            {
                return ((ScheduleType[])Enum.GetValues(typeof(ScheduleType))).ToList();
            }
        }

        private ScheduleType _selectedScheduleType;
        public ScheduleType SelectedScheduleType
        {
            get { return _selectedScheduleType; }
            set
            {
                _selectedScheduleType = value;
                updateScheduleType();
            }
        }

        public IEnumerable<int> NumTimesOptions { get; private set; }
        public int SelectedNumTimesToLoop
        {
            get { return TimeManager.Instance.Schedule.NumTimesToLoop; }
            set { TimeManager.Instance.Schedule.NumTimesToLoop = value; }
        }

        public bool ShowNumTimesOptions
        {
            get
            {
                return _selectedScheduleType == ScheduleType.NumTimes;
            }
        }

        public TimeSpan ScheduleStartTime
        {
            get { return TimeManager.Instance.Schedule.StartTime; }
            set
            {
                if (value >= ScheduleEndTime)
                {
                    // Start time is after end time, revert to previous value
                    RaisePropertyChanged(nameof(ScheduleStartTime));
                }
                else
                {
                    TimeManager.Instance.Schedule.StartTime = value;
                }
            }
        }

        public TimeSpan ScheduleEndTime
        {
            get { return TimeManager.Instance.Schedule.EndTime; }
            set
            {
                if (value <= ScheduleStartTime)
                {
                    // End time is before start time, revert to previous value
                    RaisePropertyChanged(nameof(ScheduleEndTime));
                }
                else
                {
                    TimeManager.Instance.Schedule.EndTime = value;
                }
            }
        }

        public bool ShowScheduleTimeOptions
        {
            get
            {
                return _selectedScheduleType == ScheduleType.Scheduled;
            }
        }

        public bool SundaySelected
        {
            get { return scheduleContainsDay(DayOfWeek.Sunday); }
            set { toggleDayInSchedule(DayOfWeek.Sunday); }
        }

        public bool MondaySelected
        {
            get { return scheduleContainsDay(DayOfWeek.Monday); }
            set { toggleDayInSchedule(DayOfWeek.Monday); }
        }

        public bool TuesdaySelected
        {
            get { return scheduleContainsDay(DayOfWeek.Tuesday); }
            set { toggleDayInSchedule(DayOfWeek.Tuesday); }
        }

        public bool WednesdaySelected
        {
            get { return scheduleContainsDay(DayOfWeek.Wednesday); }
            set { toggleDayInSchedule(DayOfWeek.Wednesday); }
        }

        public bool ThursdaySelected
        {
            get { return scheduleContainsDay(DayOfWeek.Thursday); }
            set { toggleDayInSchedule(DayOfWeek.Thursday); }
        }

        public bool FridaySelected
        {
            get { return scheduleContainsDay(DayOfWeek.Friday); }
            set { toggleDayInSchedule(DayOfWeek.Friday); }
        }

        public bool SaturdaySelected
        {
            get { return scheduleContainsDay(DayOfWeek.Saturday); }
            set { toggleDayInSchedule(DayOfWeek.Saturday); }
        }

        public void UpdateModes()
        {
            TimeManager.Instance.UpdateModes(Modes.Select(modeVM => modeVM.Mode).ToList());
        }

        public void SaveModeChanges(ModeVM newModeVM)
        {
            if (!newModeVM.SaveChanges())
            {
                // If mode is null then the user was adding a new mode and cancelled the add. Remove the mode from the list.
                // Otherwise, do nothing
                if (newModeVM.Mode == null)
                {
                    Modes.Remove(Modes.First(modeVM => modeVM.Mode == null));
                }
            }
            else
            {
                // The mode being saved doesn't exist in our list yet. Add it now.
                if (Modes.FirstOrDefault(modeVM => modeVM.Mode.Id == newModeVM.Mode.Id) == null)
                {
                    Modes.Add(newModeVM);
                }

                // Always update the modes so we get the changes
                UpdateModes();
            }

            ShowAddModeButton = true;
            RaisePropertyChanged(nameof(ShowAddModeButton));
        }

        public void BeginAddNewMode()
        {
            Modes.Add(new ModeVM());

            ShowAddModeButton = false;
            RaisePropertyChanged(nameof(ShowAddModeButton));
        }

        public void DeleteMode(ModeVM modeVMToDelete)
        {
            ModeVM modeToDelete = Modes.FirstOrDefault(modeVM => modeVM.Mode.Id == modeVMToDelete.Mode.Id);

            if (modeToDelete != null)
            {
                Modes.Remove(Modes.First(modeVM => modeVM.Mode.Id == modeVMToDelete.Mode.Id));
                UpdateModes();
            }
        }

        public void CancelModeChanges(ModeVM modeVM)
        {
            modeVM.CancelChanges();

            // If mode is null then the user was adding a new mode and cancelled the add. Remove the mode from the list.
            if (modeVM.Mode == null)
            {
                Modes.Remove(Modes.First(mvm => mvm.Mode == null));
            }

            ShowAddModeButton = true;
            RaisePropertyChanged(nameof(ShowAddModeButton));
        }

        private bool scheduleContainsDay(DayOfWeek day)
        {
            bool containsDay = false;
            lock (toggleDayLock)
            {
                containsDay = TimeManager.Instance.Schedule.Days.Contains(day);
            }

            return containsDay;
        }

        private void toggleDayInSchedule(DayOfWeek day)
        {
            lock (toggleDayLock)
            {
                if (scheduleContainsDay(day))
                {
                    TimeManager.Instance.Schedule.Days.Remove(day);
                }
                else
                {
                    TimeManager.Instance.Schedule.Days.Add(day);
                }
            }
        }

        private void updateScheduleType()
        {
            RaisePropertyChanged(nameof(ShowNumTimesOptions));
            RaisePropertyChanged(nameof(ShowScheduleTimeOptions));
            TimeManager.Instance.Schedule.ScheduleType = _selectedScheduleType;
        }

        private object toggleDayLock = new Object();
    }
}
