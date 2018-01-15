using SitStandTimer.Models;
using SitStandTimer.ViewModels;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace SitStandTimer
{
    public sealed partial class ConfigurationPage : PageBase
    {
        public ConfigurationPage()
        {
            this.InitializeComponent();
            DataContext = ViewModel = new ConfigurationPageVM();
        }

        public ConfigurationPageVM ViewModel { get; private set; }

        private void EditMode(object sender, RoutedEventArgs e)
        {

        }

        private void DeleteMode(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            ModeVM mode = clickedButton.DataContext as ModeVM;
            ViewModel.DeleteMode(mode);
        }

        private void SaveModeChanges(object sender, RoutedEventArgs args)
        {
            Button clickedButton = sender as Button;
            ModeVM mode = clickedButton.DataContext as ModeVM;
            ViewModel.SaveModeChanges(mode);
        }

        private void CancelModeChanges(object sender, RoutedEventArgs args)
        {
            Button clickedButton = sender as Button;
            ModeVM mode = clickedButton.DataContext as ModeVM;
            ViewModel.CancelModeChanges(mode);
        }

        private void BeginAddMode(object sender, RoutedEventArgs args)
        {
            ViewModel.BeginAddNewMode();
        }

        private void FocusRightOnEnterKeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                FocusManager.TryMoveFocus(FocusNavigationDirection.Right);
            }
        }
    }
}
