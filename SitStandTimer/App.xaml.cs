using Newtonsoft.Json;
using SitStandTimer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace SitStandTimer
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;

            // Try to register the app for background execution
            var registerTask = registerTimeBackgroundTaskAsync();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            AppFrame rootFrame = Window.Current.Content as AppFrame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new AppFrame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            // Always restore state from disk. 
            // To simplify, I'm going to do a bad thing and block the main thread while the disk read happens.
            // Since this happens before Window.Current.Activate is called, the splash screen should stay up and the user won't see any weird UI.
            try
            {
                Task restoreTask = restoreStateFromDisk();
                restoreTask.Wait();
            }
            catch (Exception)
            {
                // A FileNotFoundException is expected if there is no saved state file.
                // Ignore the error because we don't have telemetry hooked up yet to log it and we don't want the whole app launch cancelled.
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    Type targetPageType = TimeManager.Instance.HasMultipleModes ? typeof(MainPage) : typeof(ConfigurationPage);

                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(targetPageType, e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }
        }

        protected override void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            base.OnBackgroundActivated(args);

            BackgroundTaskDeferral deferral = args.TaskInstance.GetDeferral();

            Task backgroundWork = Task.Run(async () =>
            {
                // Make sure both background tasks are registered
                Task restoreStateTask = restoreStateFromDisk();
                Task backgroundRegistrationTask = registerTimeBackgroundTaskAsync();

                try
                {
                    await restoreStateTask;
                }
                catch (Exception)
                {
                    // A FileNotFoundException is expected if the save state file doesn't exist (although at this point it should)
                    // Carry on even if we are in the wrong state
                    // This app doesn't have telemetry hooked up yet so just eat the exception
                    // TODO: add HockeyApp telemetry
                }

                // Have the time manager schedule any notifications that should happen before the next time the background task runs
                TimeManager.Instance.ScheduleNotifications();

                await saveStateToDisk();
                await backgroundRegistrationTask;
            }).ContinueWith(innerTask =>
            {
                deferral.Complete();
            });
        }

        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            SuspendingDeferral deferral = e.SuspendingOperation.GetDeferral();
            Task saveStateTask = saveStateToDisk().ContinueWith(innerTask =>
            {
                deferral.Complete();
            });
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        private Task registerTimeBackgroundTaskAsync()
        {
            return Task.Run(async () => 
            {
                BackgroundAccessStatus backgroundAccess = await BackgroundExecutionManager.RequestAccessAsync();

                if (backgroundAccess == BackgroundAccessStatus.AlwaysAllowed ||
                    backgroundAccess == BackgroundAccessStatus.AllowedSubjectToSystemPolicy)
                {
                    bool needToRegisterTimeTask = true;
                    bool needToRegisterUserPresentTask = true;
                    foreach (KeyValuePair<Guid, IBackgroundTaskRegistration> task in BackgroundTaskRegistration.AllTasks)
                    {
                        if (task.Value.Name == _timeBackgroundTaskName)
                        {
                            needToRegisterTimeTask = false;
                        }
                        else if (task.Value.Name == _userPresentBackgroundTaskName)
                        {
                            needToRegisterUserPresentTask = false;
                        }
                    }

                    // Only register if this task has not previously been registered
                    if (needToRegisterTimeTask)
                    {
                        // Set the task to run every 15 minutes (guarenteed to run at least every 30 min).
                        BackgroundTaskBuilder taskBuilder = new BackgroundTaskBuilder();
                        taskBuilder.Name = _timeBackgroundTaskName;
                        taskBuilder.SetTrigger(new TimeTrigger(15, false));

                        taskBuilder.Register();
                    }

                    if (needToRegisterUserPresentTask)
                    {
                        // Set the task to run whenever the user becomes present. The intention is to have this run when the system turns on so we can register the time notification.
                        BackgroundTaskBuilder taskBuilder = new BackgroundTaskBuilder();
                        taskBuilder.Name = _userPresentBackgroundTaskName;
                        taskBuilder.SetTrigger(new SystemTrigger(SystemTriggerType.UserPresent, false));

                        taskBuilder.Register();
                    }
                }
            });
        }

        private Task saveStateToDisk()
        {
            return Task.Run(async () =>
            {
                // Save the current mode information to disk
                SaveStateModel saveInfo = TimeManager.Instance.GetCurrentModeInfo();

                // Not backed up to the cloud for now. Haven't thought through the details of that.
                StorageFolder folder = ApplicationData.Current.LocalCacheFolder;
                StorageFile saveFile = await folder.CreateFileAsync(_saveFileName, CreationCollisionOption.ReplaceExisting);
                
                using (Stream outputStream = await saveFile.OpenStreamForWriteAsync())
                using (StreamWriter output = new StreamWriter(outputStream))
                {
                    output.Write(JsonConvert.SerializeObject(saveInfo));
                }
            });
        }

        private Task restoreStateFromDisk()
        {
            return Task.Run(async () =>
            {
                try
                {
                    StorageFolder folder = ApplicationData.Current.LocalCacheFolder;
                    StorageFile saveFile = await folder.GetFileAsync(_saveFileName);

                    SaveStateModel savedState = null;
                    using (Stream inputStream = await saveFile.OpenStreamForReadAsync())
                    using (StreamReader input = new StreamReader(inputStream))
                    {
                        string json = input.ReadToEnd();
                        savedState = JsonConvert.DeserializeObject<SaveStateModel>(json);
                    }

                    TimeManager.Instance.Initialize(savedState);
                }
                catch (FileNotFoundException)
                {
                    // Ignore because it just means that there was no save file present
                    // TODO: how to handle other exception types?? Need some logging through HockeyApp
                }

                // Refresh the notification queue just to make sure everything is in sync.
                TimeManager.Instance.ScheduleNotifications();
            });
        }

        private const string _timeBackgroundTaskName = "SitStandTimerBackgroundTimeTask";
        private const string _userPresentBackgroundTaskName = "SitStandTimerBackgroundUserPresentTask";
        private const string _saveFileName = "SitStandTimer_SaveState.json";
    }
}
