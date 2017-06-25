using System;
using System.ComponentModel;

namespace SitStandTimer.ViewModels
{
    /// <summary>
    /// All view models should inherit from this class, which provides a system for notifying the view of changes to the VM data.
    /// </summary>
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
