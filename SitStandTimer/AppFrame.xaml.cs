using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace SitStandTimer
{
    public sealed partial class AppFrame : Frame
    {
        public AppFrame()
        {
            this.InitializeComponent();
        }

        private void ToggleHamburgerMenu(object sender, RoutedEventArgs e)
        {
            // Since the SplitView is defined in the style's template, it is not accesssible as a variable here.
            // We need to find it first and then store a reference for future use
            if (_appFrameSplitView == null)
            {
                _appFrameSplitView = findSplitView(this);
            }

            _appFrameSplitView.IsPaneOpen = !_appFrameSplitView.IsPaneOpen;
        }

        private void NavToConfigurationPage(object sender, RoutedEventArgs e)
        {
            Navigate(typeof(ConfigurationPage));
        }

        private void NavToMainPage(object sender, RoutedEventArgs e)
        {
            Navigate(typeof(MainPage));
        }

        /// <summary>
        /// Recursively searches through the visual tree using the parent parameter as the starting point.
        /// Returns a reference to the SplitView object if found, null otherwise.
        /// </summary>
        private SplitView findSplitView(DependencyObject obj)
        {
            if (obj is SplitView)
            {
                // Found it. There should be only one SplitView.
                return obj as SplitView;
            }

            SplitView retVal = null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);

                retVal = findSplitView(child);
                if (retVal != null)
                {
                    break;
                }
            }

            return retVal;
        }

        private SplitView _appFrameSplitView;
    }
}
