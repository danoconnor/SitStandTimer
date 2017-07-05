﻿using SitStandTimer.Models;
using SitStandTimer.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SitStandTimer
{
    public sealed partial class ConfigurationPage : Page
    {
        public ConfigurationPage()
        {
            this.InitializeComponent();
            DataContext = ViewModel = new ConfigurationPageVM();
        }

        public ConfigurationPageVM ViewModel { get; private set; }

        private void DeleteMode(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            ModeModel mode = clickedButton.DataContext as ModeModel;
            ViewModel.DeleteMode(mode);
        }

        private void BeginAddMode(object sender, RoutedEventArgs args)
        {
            // Need to load the elements
            FindName(nameof(AddModePanel));
            AddModePanel.Visibility = Visibility.Visible;
            AddModeButton.Visibility = Visibility.Collapsed;
        }

        private void SaveNewMode(object sender, RoutedEventArgs args)
        {
            ViewModel.SaveNewMode();

            // Handle hiding and unloading the add mode elements
            CancelAddNewMode(sender, args);
        }

        private void CancelAddNewMode(object sender, RoutedEventArgs args)
        {
            ViewModel.ClearNewModeStrings();

            AddModePanel.Visibility = Visibility.Collapsed;
            AddModeButton.Visibility = Visibility.Visible;
            UnloadObject(AddModePanel);
        }
    }
}
