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
            Modes = new ObservableCollection<ModeVM>();
            ShowAddModeButton = true;
            
            foreach (ModeModel mode in TimeManager.Instance.Modes)
            {
                Modes.Add(new ModeVM(mode));
            }
        }

        public ObservableCollection<ModeVM> Modes { get; set; }

        public bool ShowAddModeButton { get; private set; }

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
    }
}
